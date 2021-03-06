//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Markup;
using Microsoft.WindowsAPICodePack.Dialogs;
using MS.WindowsAPICodePack.Internal;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Dialogs.Controls;

namespace Microsoft.WindowsAPICodePack.Dialogs
{
    /// <summary>
    /// Defines the abstract base class for the common file dialogs.
    /// </summary>
    [ContentProperty("Controls")]
    public abstract class CommonFileDialog : IDialogControlHost, IDisposable
    {
        /// <summary>
        /// The collection of names selected by the user.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields", Justification="This is an internal field used by the CommonOpenFileDialog and possibly other dialogs deriving from this base class.")]
        protected readonly Collection<string> fileNames;
        internal readonly Collection<IShellItem> items;
        internal DialogShowState showState =
            DialogShowState.PreShow;

        private IFileDialog nativeDialog;
        private IFileDialogCustomize customize;
        private NativeDialogEventSink nativeEventSink;
        private bool? canceled;
        private bool resetSelections;
        private IntPtr parentWindow = IntPtr.Zero;

        /// <summary>
        /// Contains a common error message string shared by classes that 
        /// inherit from this class.
        /// </summary>
        protected const string IllegalPropertyChangeString =
            " cannot be changed while dialog is showing";

        #region Constructors

        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        protected CommonFileDialog()
        {
            if (!CoreHelpers.RunningOnVista)
                throw new PlatformNotSupportedException(
                    "Common File Dialog requires Windows Vista or later.");

            fileNames = new Collection<string>();
            items = new Collection<IShellItem>();
            filters = new CommonFileDialogFilterCollection();
            controls = new CommonFileDialogControlCollection<CommonFileDialogControl>(this);
        }

        /// <summary>
        /// Creates a new instance of this class with the specified title.
        /// </summary>
        /// <param name="title">The title to display in the dialog.</param>
        protected CommonFileDialog(string title)
            : this()
        {
            this.title = title;
        }

        #endregion

        // Template method to allow derived dialog to create actual
        // specific COM coclass (e.g. FileOpenDialog or FileSaveDialog).
        internal abstract void InitializeNativeFileDialog();
        internal abstract IFileDialog GetNativeFileDialog();
        internal abstract void PopulateWithFileNames(
            Collection<string> names);
        internal abstract void PopulateWithIShellItems(
            Collection<IShellItem> shellItems);
        internal abstract void CleanUpNativeFileDialog();
        internal abstract ShellNativeMethods.FOS GetDerivedOptionFlags(ShellNativeMethods.FOS flags);

        #region Public API

        // Events.
        /// <summary>
        /// Raised just before the dialog is about to return with a result. Occurs when the user clicks on the Open 
        /// or Save button on a file dialog box. 
        /// </summary>
        public event CancelEventHandler FileOk;
        /// <summary>
        /// Raised just before the user navigates to a new folder.
        /// </summary>
        public event EventHandler<CommonFileDialogFolderChangeEventArgs> FolderChanging;
        /// <summary>
        /// Raised when the user navigates to a new folder.
        /// </summary>
        public event EventHandler FolderChanged;
        /// <summary>
        /// Raised when the user changes the selection in the dialog's view.
        /// </summary>
        public event EventHandler SelectionChanged;
        /// <summary>
        /// Raised when the dialog is opened to notify the application of the initial chosen filetype.
        /// </summary>
        public event EventHandler FileTypeChanged;
        /// <summary>
        /// Raised when the dialog is opening.
        /// </summary>
        public event EventHandler DialogOpening;

        private CommonFileDialogControlCollection<CommonFileDialogControl> controls;
        /// <summary>
        /// Gets the collection of controls for the dialog.
        /// </summary>
        public CommonFileDialogControlCollection<CommonFileDialogControl> Controls
        {
            get { return controls; }
        }

        private CommonFileDialogFilterCollection filters;
        /// <summary>
        /// Gets the filters used by the dialog.
        /// </summary>
        public CommonFileDialogFilterCollection Filters
        {
            get { return filters; }
        }

        private string title;
        /// <summary>
        /// Gets or sets the dialog title.
        /// </summary>
        /// <value>A <see cref="System.String"/> object.</value>
        public string Title
        {
            get { return title; }
            set
            {
                title = value;
                if (NativeDialogShowing)
                    nativeDialog.SetTitle(value);
            }
        }

        // This is the first of many properties that are backed by the FOS_*
        // bitflag options set with IFileDialog.SetOptions(). 
        // SetOptions() fails
        // if called while dialog is showing (e.g. from a callback).
        private bool ensureFileExists;
        /// <summary>
        /// Gets or sets a value that determines whether the file must exist beforehand.
        /// </summary>
        /// <value>A <see cref="System.Boolean"/> value. <b>true</b> if the file must exist.</value>
        /// <exception cref="System.InvalidOperationException">This property cannot be set when the dialog is visible.</exception>
        public bool EnsureFileExists
        {
            get { return ensureFileExists; }
            set
            {
                ThrowIfDialogShowing("EnsureFileExists" + IllegalPropertyChangeString);
                ensureFileExists = value;
            }
        }

        private bool ensurePathExists;
        /// <summary>
        /// Gets or sets a value that specifies whether the returned file must be in an existing folder.
        /// </summary>
        /// <value>A <see cref="System.Boolean"/> value. <b>true</b> if the file must exist.</value>
        /// <exception cref="System.InvalidOperationException">This property cannot be set when the dialog is visible.</exception>
        public bool EnsurePathExists
        {
            get { return ensurePathExists; }
            set
            {
                ThrowIfDialogShowing("EnsurePathExists" + IllegalPropertyChangeString);
                ensurePathExists = value;
            }
        }

        private bool ensureValidNames;
        /// <summary>Gets or sets a value that determines whether to validate file names.
        /// </summary>
        ///<value>A <see cref="System.Boolean"/> value. <b>true </b>to check for situations that would prevent an application from opening the selected file, such as sharing violations or access denied errors.</value>
        /// <exception cref="System.InvalidOperationException">This property cannot be set when the dialog is visible.</exception>
        /// 
        public bool EnsureValidNames
        {
            get { return ensureValidNames; }
            set
            {
                ThrowIfDialogShowing("EnsureValidNames" + IllegalPropertyChangeString);
                ensureValidNames = value;
            }
        }

        private bool ensureReadOnly;
        /// <summary>
        /// Gets or sets a value that determines whether read-only items are returned.
        /// Default value for CommonOpenFileDialog is true (allow read-only files) and 
        /// CommonSaveFileDialog is false (don't allow read-only files).
        /// </summary>
        /// <value>A <see cref="System.Boolean"/> value. <b>true</b> includes read-only items.</value>
        /// <exception cref="System.InvalidOperationException">This property cannot be set when the dialog is visible.</exception>
        public bool EnsureReadOnly
        {
            get { return ensureReadOnly; }
            set
            {
                ThrowIfDialogShowing("EnsureReadOnly" + IllegalPropertyChangeString);
                ensureReadOnly = value;
            }
        }

        private bool restoreDirectory;
        /// <summary>
        /// Gets or sets a value that determines the restore directory.
        /// </summary>
        /// <remarks></remarks>
        /// <exception cref="System.InvalidOperationException">This property cannot be set when the dialog is visible.</exception>
        public bool RestoreDirectory
        {
            get { return restoreDirectory; }
            set
            {
                ThrowIfDialogShowing("RestoreDirectory" + IllegalPropertyChangeString);
                restoreDirectory = value;
            }
        }

        private bool showPlacesList = true;
        /// <summary>
        /// Gets or sets a value that controls whether 
        /// to show or hide the list of pinned places that
        /// the user can choose.
        /// </summary>
        /// <value>A <see cref="System.Boolean"/> value. <b>true</b> if the list is visible; otherwise <b>false</b>.</value>
        /// <exception cref="System.InvalidOperationException">This property cannot be set when the dialog is visible.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "ShowPlaces", Justification="The property is for showing or hiding the _Places_ section in Vista")]
        public bool ShowPlacesList
        {

            get { return showPlacesList; }
            set
            {
                ThrowIfDialogShowing("ShowPlacesList" + IllegalPropertyChangeString);
                showPlacesList = value;
            }
        }

        private bool addToMruList = true;
        /// <summary>
        /// Gets or sets a value that controls whether to show or hide the list of places where the user has recently opened or saved items.
        /// </summary>
        /// <value>A <see cref="System.Boolean"/> value.</value>
        /// <exception cref="System.InvalidOperationException">This property cannot be set when the dialog is visible.</exception>
        public bool AddToMostRecentlyUsedList
        {
            get { return addToMruList; }
            set
            {
                ThrowIfDialogShowing("AddToMostRecentlyUsedList" + IllegalPropertyChangeString);
                addToMruList = value;
            }
        }

        private bool showHiddenItems;
        ///<summary>
        /// Gets or sets a value that controls whether to show hidden items.
        /// </summary>
        /// <value>A <see cref="System.Boolean"/> value.<b>true</b> to show the items; otherwise <b>false</b>.</value>
        /// <exception cref="System.InvalidOperationException">This property cannot be set when the dialog is visible.</exception>
        public bool ShowHiddenItems
        {
            get { return showHiddenItems; }
            set
            {
                ThrowIfDialogShowing("ShowHiddenItems" + IllegalPropertyChangeString);
                showHiddenItems = value;
            }
        }
        private bool allowPropertyEditing;
        /// <summary>
        /// Gets or sets a value that controls whether 
        /// properties can be edited.
        /// </summary>
        /// <value>A <see cref="System.Boolean"/> value. </value>
        public bool AllowPropertyEditing
        {
            get { return allowPropertyEditing; }
            set { allowPropertyEditing = value; }
        }

        private bool navigateToShortcut = true;
        ///<summary>
        /// Gets or sets a value that controls whether shortcuts should be treated as their target items, allowing an application to open a .lnk file.
        /// </summary>
        /// <value>A <see cref="System.Boolean"/> value. <b>true</b> indicates that shortcuts should be treated as their targets. </value>
        /// <exception cref="System.InvalidOperationException">This property cannot be set when the dialog is visible.</exception>
        public bool NavigateToShortcut
        {
            get { return navigateToShortcut; }
            set
            {
                ThrowIfDialogShowing("NavigateToShortcut" + IllegalPropertyChangeString);
                navigateToShortcut = value;
            }
        }

        /// <summary>
        /// Gets or sets the default file extension to be added to file names. If the value is null
        /// or String.Empty, the extension is not added to the file names.
        /// </summary>
        public string DefaultExtension
        {
            get;
            set;
        }

        /// <summary>
        /// Tries to set the File(s) Type Combo to match the value in 
        /// 'DefaultExtension'.  Only doing this if 'this' is a Save dialog 
        /// as it makes no sense to do this if only Opening a file.
        /// </summary>
        /// 
        /// <param name="dialog">The native/IFileDialog instance.</param>
        /// 
        private void SyncFileTypeComboToDefaultExtension(IFileDialog dialog)
        {
            // make sure it's a Save dialog and that there is a default 
            // extension to sync to.
            if (!(this is CommonSaveFileDialog) || DefaultExtension == null ||
                filters.Count <= 0)
            {
                return;
            }

            // The native version of SetFileTypeIndex() requires an
            // unsigned integer as its parameter. This (having it be defined
            // as a uint right up front) avoids a cast, and the potential 
            // problems of casting a signed value to an unsigned one.
            uint filtersCounter = 0;

            CommonFileDialogFilter filter = null;

            for (filtersCounter = 0; filtersCounter < filters.Count;
                filtersCounter++)
            {
                filter = (CommonFileDialogFilter)filters[(int)filtersCounter];

                if (filter.Extensions.Contains(DefaultExtension))
                {
                    // set the docType combo to match this 
                    // extension. property is a 1-based index.
                    dialog.SetFileTypeIndex(filtersCounter + 1);

                    // we're done, exit for
                    break;
                }
            }

        }

        /// <summary>
        /// Gets the selected filename.
        /// </summary>
        /// <value>A <see cref="System.String"/> object.</value>
        /// <exception cref="System.InvalidOperationException">This property cannot be used when multiple files are selected.</exception>
        public string FileName
        {
            get
            {
                CheckFileNamesAvailable();

                if (fileNames.Count > 1)
                    throw new InvalidOperationException("Multiple files selected - the FileNames property should be used instead.");

                string returnFilename = fileNames[0];

                // "If extension is a null reference (Nothing in Visual 
                // Basic), the returned string contains the specified 
                // path with its extension removed."  Since we do not want 
                // to remove any existing extension, make sure the 
                // DefaultExtension property is NOT null.

                // if we should, and there is one to set...
                if (!string.IsNullOrEmpty(DefaultExtension))
                {
                    returnFilename = System.IO.Path.ChangeExtension(returnFilename, DefaultExtension);
                }

                return returnFilename;
            }
        }

        /// <summary>
        /// Gets the selected item as a ShellObject.
        /// </summary>
        /// <value>A <see cref="Microsoft.WindowsAPICodePack.Shell.ShellObject"></see> object.</value>
        /// <exception cref="System.InvalidOperationException">This property cannot be used when multiple files
        /// are selected.</exception>
        public ShellObject FileAsShellObject
        {
            get
            {
                CheckFileItemsAvailable();

                if (items.Count > 1)
                    throw new InvalidOperationException("Multiple files selected - the Items property should be used instead.");

                if (items.Count == 1)
                    return ShellObjectFactory.Create(items[0]);
                else
                    return null;
            }
        }

        /// <summary>
        /// Adds a location, such as a folder, library, search connector, or known folder, to the list of
        /// places available for a user to open or save items. This method actually adds an item
        /// to the <b>Favorite Links</b> or <b>Places</b> section of the Open/Save dialog.
        /// </summary>
        /// <param name="place">The item to add to the places list.</param>
        /// <param name="location">One of the enumeration values that indicates placement of the item in the list.</param>
        public void AddPlace(ShellContainer place, FileDialogAddPlaceLocation location)
        {
            // Get our native dialog
            if (nativeDialog == null)
            {
                InitializeNativeFileDialog();
                nativeDialog = GetNativeFileDialog();
            }

            // Add the shellitem to the places list
            if (nativeDialog != null)
                nativeDialog.AddPlace(((ShellObject)place).NativeShellItem, (ShellNativeMethods.FDAP)location);
        }

        /// <summary>
        /// Adds a location (folder, library, search connector, known folder) to the list of
        /// places available for the user to open or save items. This method actually adds an item
        /// to the <b>Favorite Links</b> or <b>Places</b> section of the Open/Save dialog. Overload method
        /// takes in a string for the path.
        /// </summary>
        /// <param name="path">The item to add to the places list.</param>
        /// <param name="location">One of the enumeration values that indicates placement of the item in the list.</param>
        public void AddPlace(string path, FileDialogAddPlaceLocation location)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            // Get our native dialog
            if (nativeDialog == null)
            {
                InitializeNativeFileDialog();
                nativeDialog = GetNativeFileDialog();
            }

            // Create a native shellitem from our path
            IShellItem2 nativeShellItem;
            Guid guid = new Guid(ShellIIDGuid.IShellItem2);
            int retCode = ShellNativeMethods.SHCreateItemFromParsingName(path, IntPtr.Zero, ref guid, out nativeShellItem);

            if (!CoreErrorHelper.Succeeded(retCode))
                throw new ExternalException("Shell item could not be created.", Marshal.GetExceptionForHR(retCode));

            // Add the shellitem to the places list
            if (nativeDialog != null)
                nativeDialog.AddPlace(nativeShellItem, (ShellNativeMethods.FDAP)location);
        }

        // Null = use default directory.
        private string initialDirectory;
        /// <summary>
        /// Gets or sets the initial directory displayed when the dialog is shown. 
        /// A null or empty string indicates that the dialog is using the default directory.
        /// </summary>
        /// <value>A <see cref="System.String"/> object.</value>
        public string InitialDirectory
        {
            get { return initialDirectory; }
            set { initialDirectory = value; }
        }

        private ShellContainer initialDirectoryShellContainer;
        /// <summary>
        /// Gets or sets a location that is always selected when the dialog is opened, 
        /// regardless of previous user action. A null value implies that the dialog is using 
        /// the default location.
        /// </summary>
        public ShellContainer InitialDirectoryShellContainer
        {
            get
            {
                return initialDirectoryShellContainer;
            }
            set
            {
                initialDirectoryShellContainer = value;
            }
        }

        private string defaultDirectory;
        /// <summary>
        /// Sets the folder and path used as a default if there is not a recently used folder value available.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly", Justification = "This is following the native API")]
        public string DefaultDirectory
        {
            set { defaultDirectory = value; }
        }

        private ShellContainer defaultDirectoryShellContainer;
        /// <summary>
        /// Sets the location (<see cref="Microsoft.WindowsAPICodePack.Shell.ShellContainer">ShellContainer</see> 
        /// used as a default if there is not a recently used folder value available.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly", Justification="This is following the native API")]
        public ShellContainer DefaultDirectoryShellContainer
        {
            set { defaultDirectoryShellContainer = value; }
        }

        // Null = use default identifier.
        private Guid cookieIdentifier;
        /// <summary>
        /// Gets or sets a value that enables a calling application 
        /// to associate a GUID with a dialog's persisted state.
        /// </summary>
        public Guid CookieIdentifier
        {
            get { return cookieIdentifier; }
            set { cookieIdentifier = value; }
        }



        /// <summary>
        /// Displays the dialog.
        /// </summary>
        /// <returns>A <see cref="CommonFileDialogResult"/> object.</returns>
        public CommonFileDialogResult ShowDialog()
        {
            CommonFileDialogResult result;

            // Fetch derived native dialog (i.e. Save or Open).
            InitializeNativeFileDialog();
            nativeDialog = GetNativeFileDialog();

            // Apply outer properties to native dialog instance.
            ApplyNativeSettings(nativeDialog);
            InitializeEventSink(nativeDialog);

            // Clear user data if Reset has been called 
            // since the last show.
            if (resetSelections)
            {
                resetSelections = false;
            }

            // Show dialog.
            showState = DialogShowState.Showing;
            int hresult = nativeDialog.Show(parentWindow);
            showState = DialogShowState.Closed;

            // Create return information.
            if (CoreErrorHelper.Matches(hresult, (int)HRESULT.ERROR_CANCELLED))
            {
                canceled = true;
                result = CommonFileDialogResult.Cancel;
                fileNames.Clear();
            }
            else
            {
                canceled = false;
                result = CommonFileDialogResult.OK;

                // Populate filenames if user didn't cancel.
                PopulateWithFileNames(fileNames);

                // Populate the actual IShellItems
                PopulateWithIShellItems(items);
            }

            return result;
        }
        /// <summary>
        /// Removes the current selection.
        /// </summary>
        public void ResetUserSelections()
        {
            resetSelections = true;
        }

        #endregion

        #region Configuration

        private void InitializeEventSink(IFileDialog nativeDlg)
        {
            // Check if we even need to have a sink.
            if (FileOk != null
                || FolderChanging != null
                || FolderChanged != null
                || SelectionChanged != null
                || FileTypeChanged != null
                || DialogOpening != null
                || (controls != null && controls.Count > 0))
            {
                uint cookie;
                nativeEventSink = new NativeDialogEventSink(this);
                nativeDlg.Advise(nativeEventSink, out cookie);
                nativeEventSink.Cookie = cookie;
            }
        }

        private void ApplyNativeSettings(IFileDialog dialog)
        {
            Debug.Assert(dialog != null, "No dialog instance to configure");

            if (parentWindow == IntPtr.Zero)
            {
                if (System.Windows.Application.Current != null && System.Windows.Application.Current.MainWindow != null)
                    parentWindow = (new WindowInteropHelper(System.Windows.Application.Current.MainWindow)).Handle;
                else if (System.Windows.Forms.Application.OpenForms.Count > 0)
                    parentWindow = System.Windows.Forms.Application.OpenForms[0].Handle;
            }

            Guid guid = new Guid(ShellIIDGuid.IShellItem2);

            // Apply option bitflags.
            dialog.SetOptions(CalculateNativeDialogOptionFlags());

            // Other property sets.
            if (title != null)
                dialog.SetTitle(title);
            if (initialDirectoryShellContainer != null)
            {
                dialog.SetFolder(((ShellObject)initialDirectoryShellContainer).NativeShellItem);
            }
            if (defaultDirectoryShellContainer != null)
            {
                dialog.SetDefaultFolder(((ShellObject)defaultDirectoryShellContainer).NativeShellItem);
            }
            if (!String.IsNullOrEmpty(initialDirectory))
            {
                // Create a native shellitem from our path
                IShellItem2 initialDirectoryShellItem;
                ShellNativeMethods.SHCreateItemFromParsingName(initialDirectory, IntPtr.Zero, ref guid, out initialDirectoryShellItem);

                // If we get a real shell item back, 
                // then use that as the initial folder - otherwise,
                // we'll allow the dialog to revert to the default folder. 
                // (OR should we fail loudly?)
                if (initialDirectoryShellItem != null)
                    dialog.SetFolder(initialDirectoryShellItem);
            }
            if (!string.IsNullOrEmpty(defaultDirectory))
            {
                // Create a native shellitem from our path
                IShellItem2 defaultDirectoryShellItem;
                ShellNativeMethods.SHCreateItemFromParsingName(defaultDirectory, IntPtr.Zero, ref guid, out defaultDirectoryShellItem);

                // If we get a real shell item back, 
                // then use that as the initial folder - otherwise,
                // we'll allow the dialog to revert to the default folder. 
                // (OR should we fail loudly?)
                if (defaultDirectoryShellItem != null)
                    dialog.SetDefaultFolder(defaultDirectoryShellItem);
            }

            // Apply file type filters, if available.
            if (filters.Count > 0)
            {
                dialog.SetFileTypes(
                    (uint)filters.Count,
                    filters.GetAllFilterSpecs());

                SyncFileTypeComboToDefaultExtension(dialog);
            }

            if (cookieIdentifier != Guid.Empty)
                dialog.SetClientGuid(ref cookieIdentifier);

            // Set the default extension
            if (!string.IsNullOrEmpty(DefaultExtension))
                dialog.SetDefaultExtension(DefaultExtension);

        }

        private ShellNativeMethods.FOS CalculateNativeDialogOptionFlags()
        {
            // We start with only a few flags set by default, 
            // then go from there based on the current state
            // of the managed dialog's property values.
            ShellNativeMethods.FOS flags =
                ShellNativeMethods.FOS.FOS_NOTESTFILECREATE;

            // Call to derived (concrete) dialog to 
            // set dialog-specific flags.
            flags = GetDerivedOptionFlags(flags);

            // Apply other optional flags.
            if (ensureFileExists)
                flags |= ShellNativeMethods.FOS.FOS_FILEMUSTEXIST;
            if (ensurePathExists)
                flags |= ShellNativeMethods.FOS.FOS_PATHMUSTEXIST;
            if (!ensureValidNames)
                flags |= ShellNativeMethods.FOS.FOS_NOVALIDATE;
            if (!EnsureReadOnly)
                flags |= ShellNativeMethods.FOS.FOS_NOREADONLYRETURN;
            if (restoreDirectory)
                flags |= ShellNativeMethods.FOS.FOS_NOCHANGEDIR;
            if (!showPlacesList)
                flags |= ShellNativeMethods.FOS.FOS_HIDEPINNEDPLACES;
            if (!addToMruList)
                flags |= ShellNativeMethods.FOS.FOS_DONTADDTORECENT;
            if (showHiddenItems)
                flags |= ShellNativeMethods.FOS.FOS_FORCESHOWHIDDEN;
            if (!navigateToShortcut)
                flags |= ShellNativeMethods.FOS.FOS_NODEREFERENCELINKS;
            return flags;
        }

        #endregion

        #region IDialogControlHost Members

        private static void GenerateNotImplementedException()
        {
            throw new NotImplementedException(
                "The method or operation is not implemented.");
        }

        bool IDialogControlHost.IsCollectionChangeAllowed()
        {
            return true;
        }

        void IDialogControlHost.ApplyCollectionChanged()
        {
            // Query IFileDialogCustomize interface before adding controls
            GetCustomizedFileDialog();
            // Populate all the custom controls and add them to the dialog
            foreach (CommonFileDialogControl control in controls)
            {
                if (!control.IsAdded)
                {
                    control.HostingDialog = this;
                    control.Attach(customize);
                    control.IsAdded = true;
                }
            }

        }

        bool IDialogControlHost.IsControlPropertyChangeAllowed(string propertyName, DialogControl control)
        {
            CommonFileDialog.GenerateNotImplementedException();
            return false;
        }

        void IDialogControlHost.ApplyControlPropertyChange(string propertyName, DialogControl control)
        {
            if (propertyName == "Text")
            {
                customize.SetControlLabel(control.Id, ((CommonFileDialogControl)control).Text);
            }
            else if (propertyName == "Visible")
            {
                CommonFileDialogControl dialogControl = control as CommonFileDialogControl;
                ShellNativeMethods.CDCONTROLSTATE state;

                customize.GetControlState(control.Id, out state);

                if (dialogControl.Visible == true)
                    state |= ShellNativeMethods.CDCONTROLSTATE.CDCS_VISIBLE;
                else if (dialogControl.Visible == false)
                {
                    state &= ~ShellNativeMethods.CDCONTROLSTATE.CDCS_VISIBLE;
                }

                customize.SetControlState(control.Id, state);
            }
            else if (propertyName == "Enabled")
            {
                CommonFileDialogControl dialogControl = control as CommonFileDialogControl;
                ShellNativeMethods.CDCONTROLSTATE state;

                customize.GetControlState(control.Id, out state);

                if (dialogControl.Enabled == true)
                    state |= ShellNativeMethods.CDCONTROLSTATE.CDCS_ENABLED;
                else if (dialogControl.Enabled == false)
                {
                    state &= ~ShellNativeMethods.CDCONTROLSTATE.CDCS_ENABLED;
                }

                customize.SetControlState(control.Id, state);
            }
            else if (propertyName == "SelectedIndex")
            {
                if (control is CommonFileDialogRadioButtonList)
                {
                    CommonFileDialogRadioButtonList list = control as CommonFileDialogRadioButtonList;
                    customize.SetSelectedControlItem(control.Id, list.SelectedIndex);
                }
                else if (control is CommonFileDialogComboBox)
                {
                    CommonFileDialogComboBox box = control as CommonFileDialogComboBox;
                    customize.SetSelectedControlItem(control.Id, box.SelectedIndex);
                }
            }
            else if (propertyName == "IsChecked")
            {
                if (control is CommonFileDialogCheckBox)
                {
                    CommonFileDialogCheckBox checkBox = control as CommonFileDialogCheckBox;
                    customize.SetCheckButtonState(control.Id, checkBox.IsChecked);
                }
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Ensures that the user has selected one or more files.
        /// </summary>
        /// <permission cref="System.InvalidOperationException">
        /// The dialog has not been dismissed yet or the dialog was cancelled.
        /// </permission>
        protected void CheckFileNamesAvailable()
        {
            if (showState != DialogShowState.Closed)
                throw new InvalidOperationException(
                    "Filename not available - dialog has not closed yet.");

            if (canceled.GetValueOrDefault())
                throw new InvalidOperationException(
                    "Filename not available - dialog was canceled.");

            Debug.Assert(fileNames.Count != 0,
              "FileNames empty - shouldn't happen unless dialog canceled or not yet shown.");
        }

        /// <summary>
        /// Ensures that the user has selected one or more files.
        /// </summary>
        /// <permission cref="System.InvalidOperationException">
        /// The dialog has not been dismissed yet or the dialog was cancelled.
        /// </permission>
        protected void CheckFileItemsAvailable()
        {
            if (showState != DialogShowState.Closed)
                throw new InvalidOperationException(
                    "Filename not available - dialog has not closed yet.");

            if (canceled.GetValueOrDefault())
                throw new InvalidOperationException(
                    "Filename not available - dialog was canceled.");

            Debug.Assert(items.Count != 0,
              "Items list empty - shouldn't happen unless dialog canceled or not yet shown.");
        }

        static IntPtr GetHandleFromWindow(Window window)
        {
            if (window == null)
                return IntPtr.Zero;

            return (new WindowInteropHelper(window)).Handle;
        }

        #endregion

        #region Helpers

        private bool NativeDialogShowing
        {
            get
            {
                return (nativeDialog != null)
                    && (showState == DialogShowState.Showing ||
                    showState == DialogShowState.Closing);
            }
        }

        internal static string GetFileNameFromShellItem( IShellItem item )
        {
            string filename = null;
            IntPtr pszString = IntPtr.Zero;
            HRESULT hr = item.GetDisplayName( ShellNativeMethods.SIGDN.SIGDN_DESKTOPABSOLUTEPARSING, out pszString );
            if (hr == HRESULT.S_OK && pszString != IntPtr.Zero)
            {
                filename = Marshal.PtrToStringAuto(pszString);
                Marshal.FreeCoTaskMem(pszString);
            }
            return filename;
        }

        internal static IShellItem GetShellItemAt(IShellItemArray array, int i)
        {
            IShellItem result;
            uint index = (uint)i;
            array.GetItemAt(index, out result);
            return result;
        }

        /// <summary>
        /// Throws an exception when the dialog is showing preventing
        /// a requested change to a property or the visible set of controls.
        /// </summary>
        /// <param name="message">The message to include in the exception.</param>
        /// <permission cref="System.InvalidOperationException"> The dialog is in an
        /// invalid state to perform the requested operation.</permission>
        protected void ThrowIfDialogShowing(string message)
        {
            if (NativeDialogShowing)
                throw new InvalidOperationException(message);
        }
        /// <summary>
        /// Get the IFileDialogCustomize interface, preparing to add controls.
        /// </summary>
        private void GetCustomizedFileDialog()
        {
            if (customize == null)
            {
                if (nativeDialog == null)
                {
                    InitializeNativeFileDialog();
                    nativeDialog = GetNativeFileDialog();
                }
                customize = (IFileDialogCustomize)nativeDialog;
            }
        }
        #endregion

        #region CheckChanged handling members
        /// <summary>
        /// Raises the <see cref="CommonFileDialog.FileOk"/> event just before the dialog is about to return with a result.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected virtual void OnFileOk(CancelEventArgs e)
        {
            CancelEventHandler handler = FileOk;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        /// <summary>
        /// Raises the <see cref="FolderChanging"/> to stop navigation to a particular location.
        /// </summary>
        /// <param name="e">Cancelable event arguments.</param>
        protected virtual void OnFolderChanging(CommonFileDialogFolderChangeEventArgs e)
        {
            EventHandler<CommonFileDialogFolderChangeEventArgs> handler = FolderChanging;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        /// <summary>
        /// Raises the <see cref="CommonFileDialog.FolderChanged"/> event when the user navigates to a new folder.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected virtual void OnFolderChanged(EventArgs e)
        {
            EventHandler handler = FolderChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        /// <summary>
        /// Raises the <see cref="CommonFileDialog.SelectionChanged"/> event when the user changes the selection in the dialog's view.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected virtual void OnSelectionChanged(EventArgs e)
        {
            EventHandler handler = SelectionChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        /// <summary>
        /// Raises the <see cref="CommonFileDialog.FileTypeChanged"/> event when the dialog is opened to notify the 
        /// application of the initial chosen filetype.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected virtual void OnFileTypeChanged(EventArgs e)
        {
            EventHandler handler = FileTypeChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        /// <summary>
        /// Raises the <see cref="CommonFileDialog.DialogOpening"/> event when the dialog is opened.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected virtual void OnOpening(EventArgs e)
        {
            EventHandler handler = DialogOpening;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        #endregion

        #region NativeDialogEventSink Nested Class

        private class NativeDialogEventSink : IFileDialogEvents, IFileDialogControlEvents
        {
            private CommonFileDialog parent;
            private bool firstFolderChanged = true;

            public NativeDialogEventSink(CommonFileDialog commonDialog)
            {
                this.parent = commonDialog;
            }

            private uint cookie;
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification="This can be called from an application using our library")]
            public uint Cookie
            {
                get { return cookie; }
                set { cookie = value; }
            }

            public HRESULT OnFileOk(IFileDialog pfd)
            {
                CancelEventArgs args = new CancelEventArgs();
                parent.OnFileOk(args);
                return (args.Cancel ? HRESULT.S_FALSE : HRESULT.S_OK);
            }

            public HRESULT OnFolderChanging(IFileDialog pfd, IShellItem psiFolder)
            {
                CommonFileDialogFolderChangeEventArgs args =
                    new CommonFileDialogFolderChangeEventArgs(CommonFileDialog.GetFileNameFromShellItem(psiFolder));
                if (!firstFolderChanged)
                    parent.OnFolderChanging(args);
                return (args.Cancel ? HRESULT.S_FALSE : HRESULT.S_OK);
            }

            public void OnFolderChange(IFileDialog pfd)
            {
                if (firstFolderChanged)
                {
                    firstFolderChanged = false;
                    parent.OnOpening(EventArgs.Empty);
                }
                else
                    parent.OnFolderChanged(EventArgs.Empty);
            }

            public void OnSelectionChange(IFileDialog pfd)
            {
                parent.OnSelectionChanged(EventArgs.Empty);
            }

            public void OnShareViolation(
                IFileDialog pfd,
                IShellItem psi,
                out ShellNativeMethods.FDE_SHAREVIOLATION_RESPONSE pResponse)
            {
                // Do nothing: we will ignore share violations, 
                // and don't register
                // for them, so this method should never be called.
                pResponse = ShellNativeMethods.FDE_SHAREVIOLATION_RESPONSE.FDESVR_ACCEPT;
            }

            public void OnTypeChange(IFileDialog pfd)
            {
                parent.OnFileTypeChanged(EventArgs.Empty);
            }

            public void OnOverwrite(IFileDialog pfd, IShellItem psi, out ShellNativeMethods.FDE_OVERWRITE_RESPONSE pResponse)
            {
                // Don't accept or reject the dialog, keep default settings
                pResponse = ShellNativeMethods.FDE_OVERWRITE_RESPONSE.FDEOR_DEFAULT;
            }

            public void OnItemSelected(IFileDialogCustomize pfdc, int dwIDCtl, int dwIDItem)
            {
                // Find control
                DialogControl control = this.parent.controls.GetControlbyId(dwIDCtl);

                // Process ComboBox and/or RadioButtonList
                if (control is ICommonFileDialogIndexedControls)
                {
                    // Update selected item and raise SelectedIndexChanged event
                    ICommonFileDialogIndexedControls controlInterface = control as ICommonFileDialogIndexedControls;
                    controlInterface.SelectedIndex = dwIDItem;
                    controlInterface.RaiseSelectedIndexChangedEvent();
                }
                // Process Menu
                else if (control is CommonFileDialogMenu)
                {
                    CommonFileDialogMenu menu = control as CommonFileDialogMenu;

                    // Find the menu item that was clicked and invoke it's click event
                    foreach (CommonFileDialogMenuItem item in menu.Items)
                    {
                        if (item.Id == dwIDItem)
                        {
                            item.RaiseClickEvent();
                            break;
                        }
                    }
                }
            }

            public void OnButtonClicked(IFileDialogCustomize pfdc, int dwIDCtl)
            {
                // Find control
                DialogControl control = this.parent.controls.GetControlbyId(dwIDCtl);

                // Call corresponding event
                if (control is CommonFileDialogButton)
                {
                    ((CommonFileDialogButton)control).RaiseClickEvent();
                }
            }

            public void OnCheckButtonToggled(IFileDialogCustomize pfdc, int dwIDCtl, bool bChecked)
            {
                // Find control
                DialogControl control = this.parent.controls.GetControlbyId(dwIDCtl);

                // Update control and call corresponding event
                if (control is CommonFileDialogCheckBox)
                {
                    CommonFileDialogCheckBox box = control as CommonFileDialogCheckBox;
                    box.IsChecked = bChecked;
                    box.RaiseCheckedChangedEvent();
                }
            }

            public void OnControlActivating(IFileDialogCustomize pfdc, int dwIDCtl)
            {
            }
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Releases the unmanaged resources used by the CommonFileDialog class and optionally 
        /// releases the managed resources.
        /// </summary>
        /// <param name="disposing"><b>true</b> to release both managed and unmanaged resources; 
        /// <b>false</b> to release only unmanaged resources.</param>
        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                CleanUpNativeFileDialog();
            }
        }

        /// <summary>
        /// Releases the resources used by the current instance of the CommonFileDialog class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

        /// <summary>
        /// Indicates whether this feature is supported on the current platform.
        /// </summary>
        public static bool IsPlatformSupported
        {
            get
            {
                // We need Windows Vista onwards ...
                return CoreHelpers.RunningOnVista;
            }
        }
    }


}
