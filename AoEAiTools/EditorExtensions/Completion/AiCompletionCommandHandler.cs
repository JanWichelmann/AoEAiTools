using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AoEAiTools.EditorExtensions.Completion
{
    /// <summary>
    /// Triggers completion sessions.
    /// </summary>
    internal class AiCompletionCommandHandler : IOleCommandTarget
    {
        private IOleCommandTarget _nextCommandHandler;
        private ITextView _textView;
        private AiCompletionHandlerProvider _provider;
        private ICompletionSession _session;

        internal AiCompletionCommandHandler(IVsTextView textViewAdapter, ITextView textView, AiCompletionHandlerProvider provider)
        {
            // Save arguments
            _textView = textView;
            _provider = provider;

            // Add the command to the command chain
            textViewAdapter.AddCommandFilter(this, out _nextCommandHandler);
        }

        public int QueryStatus(ref Guid commandGroupGuid, uint commandCount, OLECMD[] programmCommands, IntPtr commandText)
        {
            // Pass to next command handler
            return _nextCommandHandler.QueryStatus(ref commandGroupGuid, commandCount, programmCommands, commandText);
        }

        public int Exec(ref Guid commandGroupGuid, uint commandId, uint commandExecuteOptionsCount, IntPtr variantIn, IntPtr variantOut)
        {
            if(VsShellUtilities.IsInAutomationFunction(_provider.ServiceProvider))
                return _nextCommandHandler.Exec(ref commandGroupGuid, commandId, commandExecuteOptionsCount, variantIn, variantOut);

            // Make sure the input is a char before getting it
            char typedChar = char.MinValue;
            if(commandGroupGuid == VSConstants.VSStd2K && commandId == (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
                typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(variantIn);

            // Check for a commit character
            if(commandId == (uint)VSConstants.VSStd2KCmdID.RETURN || commandId == (uint)VSConstants.VSStd2KCmdID.TAB || char.IsWhiteSpace(typedChar))
            {
                // Check for an active session
                if(_session != null && !_session.IsDismissed)
                {
                    // Is the current item in the active completion set fully selected?
                    if(_session.SelectedCompletionSet.SelectionStatus.IsSelected)
                    {
                        // Commit the current session
                        _session.Commit();

                        // Do not apply committing new lines/tabs to the buffer
                        if(commandId == (uint)VSConstants.VSStd2KCmdID.RETURN || commandId == (uint)VSConstants.VSStd2KCmdID.TAB)
                            return VSConstants.S_OK;
                    }
                    else
                    {
                        // If there is no selection, dismiss the session
                        _session.Dismiss();
                    }
                }
            }

            // Pass along the command so the char is added to the buffer
            int ret = _nextCommandHandler.Exec(ref commandGroupGuid, commandId, commandExecuteOptionsCount, variantIn, variantOut);
            if(!typedChar.Equals(char.MinValue) && (char.IsLetterOrDigit(typedChar) || typedChar == '-'))
            {
                // If there is no active session, bring up completion, else filter
                if(_session == null || _session.IsDismissed)
                {
                    // Filter if completion set is valid and not empty
                    if(TriggerCompletion())
                        _session.Filter();
                }
                else
                    _session.Filter();
                return VSConstants.S_OK;
            }
            else if(commandId == (uint)VSConstants.VSStd2KCmdID.BACKSPACE || commandId == (uint)VSConstants.VSStd2KCmdID.DELETE)
            {
                if(_session != null && !_session.IsDismissed)
                    _session.Filter();
                return VSConstants.S_OK;
            }
            return ret;
        }

        private bool TriggerCompletion()
        {
            // The caret must be in a non-projection location 
            SnapshotPoint? caretPoint = _textView.Caret.Position.Point.GetPoint(textBuffer => (!textBuffer.ContentType.IsOfType("projection")), PositionAffinity.Predecessor);
            if(!caretPoint.HasValue)
                return false;

            // Create session
            _session = _provider.CompletionBroker.CreateCompletionSession(_textView, caretPoint.Value.Snapshot.CreateTrackingPoint(caretPoint.Value.Position, PointTrackingMode.Positive), true);

            // Subscribe to the Dismissed event on the session to do some cleanup
            _session.Dismissed += this.OnSessionDismissed;

            // Run session
            _session.Start();
            return _session != null && !_session.IsDismissed;
        }

        private void OnSessionDismissed(object sender, EventArgs e)
        {
            // Delete session
            _session.Dismissed -= this.OnSessionDismissed;
            _session = null;
        }
    }
}
