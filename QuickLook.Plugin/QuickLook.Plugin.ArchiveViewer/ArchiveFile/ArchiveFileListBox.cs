// Copyright © 2017-2026 QL-Win Contributors
// Licensed under the GNU General Public License, version 3 or later.

using System;
using System.Collections.Generic;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;

namespace QuickLook.Plugin.ArchiveViewer.ArchiveFile;

/// <summary>
/// Keeps data-item automation peers when visual containers are recycled.
/// The flat list remains a List (not a falsely advertised hierarchical UIA tree).
/// </summary>
public sealed class ArchiveFileListBox : ListBox
{
    private ArchiveListPeer _peer;
    internal Action<ArchiveFileRow, bool> ExpansionRequested { get; set; }

    protected override AutomationPeer OnCreateAutomationPeer() => _peer = new ArchiveListPeer(this);

    internal void ResetAutomationPeers() => _peer?.ClearRows();

    internal void RaiseExpansionChanged(ArchiveFileRow row, bool previous) => _peer?.RaiseExpansionChanged(row, previous);

    private sealed class ArchiveListPeer(ArchiveFileListBox owner) : ListBoxAutomationPeer(owner)
    {
        private readonly Dictionary<ArchiveFileRow, WeakReference<ArchiveRowPeer>> _rows = [];

        protected override ItemAutomationPeer CreateItemAutomationPeer(object item)
        {
            var row = (ArchiveFileRow)item;
            if (_rows.TryGetValue(row, out var reference) && reference.TryGetTarget(out var peer))
                return peer;
            peer = new ArchiveRowPeer(item, this, owner);
            _rows[row] = new WeakReference<ArchiveRowPeer>(peer);
            return peer;
        }

        public void ClearRows() => _rows.Clear();

        public void RaiseExpansionChanged(ArchiveFileRow row, bool previous)
        {
            if (_rows.TryGetValue(row, out var reference) && reference.TryGetTarget(out var peer))
                peer.RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                    previous ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
                    row.IsExpanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
        }
    }

    private sealed class ArchiveRowPeer(object item, ListBoxAutomationPeer parent, ArchiveFileListBox owner)
        : ListBoxItemAutomationPeer(item, parent), IExpandCollapseProvider
    {
        private ArchiveFileRow Row => (ArchiveFileRow)Item;

        protected override string GetNameCore() => Row.Entry.Name;

        protected override string GetHelpTextCore() => $"Level {Row.Depth + 1}";

        public override object GetPattern(PatternInterface patternInterface) =>
            patternInterface == PatternInterface.ExpandCollapse && Row.HasChildren
                ? this : base.GetPattern(patternInterface);

        ExpandCollapseState IExpandCollapseProvider.ExpandCollapseState =>
            !Row.HasChildren ? ExpandCollapseState.LeafNode :
            Row.IsExpanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

        void IExpandCollapseProvider.Expand() => SetExpanded(true);
        void IExpandCollapseProvider.Collapse() => SetExpanded(false);

        private void SetExpanded(bool expanded)
        {
            owner.Dispatcher.Invoke(() =>
            {
                if (!owner.IsEnabled) throw new ElementNotEnabledException();
                if (!owner.Items.Contains(Row) || owner.ExpansionRequested == null)
                    throw new ElementNotAvailableException();
                owner.ExpansionRequested(Row, expanded);
            });
        }
    }
}
