using System;

namespace FileSystemVisitor
{
    public class ContentFindedEventArgs : EventArgs
    {
        public bool StopSearch { get; set; }
    }
}