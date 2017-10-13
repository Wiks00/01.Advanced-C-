using System;

namespace FileSystemVisitor
{
    public class FilteredContentFindedEventArgs : EventArgs
    {
        public bool StopSearch { get; set; }
        public bool RemoveItem { get; set; }
    }
}