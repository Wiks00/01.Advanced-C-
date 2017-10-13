using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FileSystemVisitor
{
    public class FileSystemVisitor : IEnumerable
    {
        #region Private Fields
        private readonly string _rootDirectory;
        private readonly Predicate<string> _folderFilter;
        private readonly Func<string, bool> _fileFilter;
        private readonly bool _defaultFolderFilter = true;
        private readonly bool _defaultFileFilter = true;
        private bool _searchStopped;
        private bool _itemRemoved;
        #endregion

        #region Events
        /// <summary>
        /// Raise action when process start
        /// </summary>
        public event Action Start;

        /// <summary>
        /// Raise action when process finish
        /// </summary>
        public event Action Finish;

        /// <summary>
        /// Raise action when process find file by granted filter
        /// </summary>
        public event EventHandler<ContentFindedEventArgs> FileFound;

        /// <summary>
        /// Raise action when process find directory
        /// </summary>
        public event EventHandler<ContentFindedEventArgs> DirectoryFound;

        /// <summary>
        /// Raise action when process find file by granted filter
        /// </summary>
        public event EventHandler<FilteredContentFindedEventArgs> FilteredFileFound;

        /// <summary>
        /// Raise action when process find directory by granted filter
        /// </summary>
        public event EventHandler<FilteredContentFindedEventArgs> FilteredDirectoryFound;
        #endregion

        #region Constructors
        protected FileSystemVisitor() { }

        /// <summary>
        /// Create new instance of FileSystemVisitor with path to root directory
        /// </summary>
        /// <param name="rootDirectory">Path to start directory</param>
        /// <exception cref="ArgumentException"></exception>
        public FileSystemVisitor(string rootDirectory) : this(rootDirectory , null, null)
        {           
        }

        /// <summary>
        /// Create new instance of FileSystemVisitor with path to root directory and filter expression for folders
        /// </summary>
        /// <param name="rootDirectory">Path to start directory</param>
        /// <param name="folderFilter">Filter expression for folders</param>
        /// <exception cref="ArgumentException"></exception>
        public FileSystemVisitor(string rootDirectory, Predicate<string> folderFilter) : this(rootDirectory, folderFilter, null)
        {
        }

        /// <summary>
        /// Create new instance of FileSystemVisitor with path to root directory and filter expression for files
        /// </summary>
        /// <param name="rootDirectory">Path to start directory</param>
        /// <param name="fileFilter">Filter expression for files</param>
        /// <exception cref="ArgumentException"></exception>
        public FileSystemVisitor(string rootDirectory, Func<string, bool> fileFilter) : this(rootDirectory, null, fileFilter)
        {
        }

        /// <summary>
        /// Create new instance of FileSystemVisitor with path to root directory and filter expression for folders and files
        /// </summary>
        /// <param name="rootDirectory">Path to start directory</param>
        /// <param name="folderFilter">Filter expression for folders</param>
        /// <param name="fileFilter">Filter expression for files</param>
        /// <exception cref="ArgumentException"></exception>
        public FileSystemVisitor(string rootDirectory, Predicate<string> folderFilter, Func<string, bool> fileFilter)
        {
            if (!Directory.Exists(rootDirectory))
            {
                throw new ArgumentException($"'{rootDirectory}' doesn't exist!", nameof(rootDirectory));
            }

            _rootDirectory = rootDirectory;

            if (ReferenceEquals(fileFilter, null))
            {
                _fileFilter = x => true;
            }
            else
            {
                _fileFilter = fileFilter;
                _defaultFileFilter = false;
            }
        
            if (ReferenceEquals(folderFilter, null))
            {
                _folderFilter = x => true;
            }
            else
            {
                _folderFilter = folderFilter;
                _defaultFolderFilter = false;
            }


        }
        #endregion

        #region Methods

        IEnumerator IEnumerable.GetEnumerator()
        {
            OnStart();

            return GetFolderContent(_rootDirectory).GetEnumerator();
        }

        protected virtual void OnStart() =>Start?.Invoke();

        protected virtual void OnFinish() => Finish?.Invoke();

        protected virtual void OnFilteredFileFound(FilteredContentFindedEventArgs e)
        {
            if (ReferenceEquals(e, null))
            {
                throw new ArgumentNullException();
            }

            FilteredFileFound?.Invoke(this, e);

            if (!_searchStopped)
            {
                _searchStopped = e.StopSearch;
            }

            _itemRemoved = e.RemoveItem;
        }

        protected virtual void OnFilteredDirectoryFound(FilteredContentFindedEventArgs e)
        {
            if (ReferenceEquals(e, null))
            {
                throw new ArgumentNullException();
            }

            FilteredDirectoryFound?.Invoke(this, e);

            if (!_searchStopped)
            {
                _searchStopped = e.StopSearch;
            }

            _itemRemoved = e.RemoveItem;
        }

        protected virtual void OnFileFound(ContentFindedEventArgs e)
        {
            if (ReferenceEquals(e, null))
            {
                throw new ArgumentNullException();
            }

            FileFound?.Invoke(this, e);

            _searchStopped = e.StopSearch;
        }

        protected virtual void OnDirectoryFound(ContentFindedEventArgs e)
        {
            if (ReferenceEquals(e, null))
            {
                throw new ArgumentNullException();
            }

            DirectoryFound?.Invoke(this, e);
            _searchStopped = e.StopSearch;
        }

        #endregion

        #region Private Methods

        private IEnumerable<string> FilterFolderContent(List<string> content)
        {
            if (ReferenceEquals(content, null))
            {
                throw new ArgumentNullException();
            }

            var filteredFolders = content.Where(Directory.Exists).Where(item => _folderFilter(item));
            var filteredFiles = content.Where(File.Exists).Where(_fileFilter);

            var filteredContent = filteredFolders.Concat(filteredFiles).ToList();

            foreach (var item in content)
            {
                if (_searchStopped)
                {
                    continue;
                }

                var fileAttributes = File.GetAttributes(item);

                if (fileAttributes.HasFlag(FileAttributes.Directory))
                {
                    OnDirectoryFound(new ContentFindedEventArgs());

                    if (filteredContent.Contains(item) && !_defaultFolderFilter)
                    {
                        OnFilteredDirectoryFound(new FilteredContentFindedEventArgs());
                    }
                }
                else
                {
                    OnFileFound(new ContentFindedEventArgs());

                    if (filteredContent.Contains(item) && !_defaultFileFilter)
                    {
                        OnFilteredFileFound(new FilteredContentFindedEventArgs());
                    }
                }

                yield return item;
            }
        }

        private IEnumerable<string> GetFolderContent(string rootDirectory)
        {
            if (ReferenceEquals(rootDirectory, null))
            {
                throw new ArgumentNullException();
            }

            if (!Directory.Exists(rootDirectory))
            {
                yield break;
            }

            var content = Directory.EnumerateFileSystemEntries(rootDirectory).ToList();

            foreach (var folderItem in FilterFolderContent(content))
            {
                if (!_itemRemoved)
                {
                    yield return folderItem;
                }

                foreach (var subFolderItem in GetFolderContent(folderItem))
                {
                    yield return subFolderItem;
                }
            }
        
            if (rootDirectory == this._rootDirectory)
            {
                OnFinish();
            }
        }

        #endregion
    }
}
