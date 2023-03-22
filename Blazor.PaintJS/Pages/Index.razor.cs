using Blazor.PaintJS.Services;
using Excubo.Blazor.Canvas;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Drawing;
using KristofferStrube.Blazor.FileSystemAccess;
using Thinktecture.Blazor.WebShare;
using Thinktecture.Blazor.WebShare.Models;
using Thinktecture.Blazor.AsyncClipboard;
using Thinktecture.Blazor.AsyncClipboard.Models;
using Thinktecture.Blazor.FileHandling;
using KristofferStrube.Blazor.FileSystem;
using Thinktecture.Blazor.Badging;

namespace Blazor.PaintJS.Pages
{
    public partial class Index
    {
        [Inject] private PaintService _paintService { get; set; } = default!;
        [Inject] private ImageService _imageService { get; set; } = default!;
        [Inject] private AsyncClipboardService _asyncClipboardService { get; set; } = default!;
        [Inject] private WebShareService _shareService { get; set; } = default!;
        [Inject] private BadgingService _badgingService { get; set; } = default!;
        [Inject] private IFileSystemAccessService _fileSystemAccessService { get; set; } = default!;
        [Inject] private FileHandlingService _fileHandlingService { get; set; } = default!;
        [Inject] public IJSRuntime JS { get; set; } = default!;

        private IJSObjectReference? _module;
        private DotNetObjectReference<Index>? _selfReference;

        protected FileSystemFileHandle? _fileHandle;

        private static FilePickerAcceptType[] _acceptedTypes = new FilePickerAcceptType[]
        {
            new FilePickerAcceptType
            {
                Accept = new Dictionary<string, string[]>
                {
                    { "image/png", new[] {".png" } }
                }
            }
        };

        private SaveFilePickerOptionsStartInWellKnownDirectory _savePickerOptions = new SaveFilePickerOptionsStartInWellKnownDirectory
        {
            StartIn = WellKnownDirectory.Pictures,
            Types = _acceptedTypes
        };

        private OpenFilePickerOptionsStartInWellKnownDirectory _openFilePickerOptions = new OpenFilePickerOptionsStartInWellKnownDirectory
        {
            Multiple = false,
            StartIn = WellKnownDirectory.Pictures,
            Types = _acceptedTypes
        };

        private bool _fileSystemAccessSupported = false;
        private bool _clipBoardApiSupported = false;
        private bool _sharedApiSupported = false;
        private bool _badgeApiSupported = false;
        private Canvas? _canvas;
        private Point? _previousPoint;
        private int _hasChanges = 0;

        // Method which is JSInvokable must be public
        [JSInvokable]
        public void OnPointerUp()
        {
            _previousPoint = null;
        }

        private async Task UpdateBage(bool reset = false)
        {
            if (_badgeApiSupported)
            {
                _hasChanges = reset ? 0 : _hasChanges + 1;
                await _badgingService.SetAppBadgeAsync(_hasChanges);
            }
        }

        [JSInvokable]
        public async Task DrawImageAsync()
        {
            await using var context = await _canvas!.GetContext2DAsync();
            await context.DrawImageAsync("image", 0, 0);
        }

        protected override async Task OnInitializedAsync()
        {
            _badgeApiSupported = await _badgingService.IsSupportedAsync();
            _fileSystemAccessSupported = await _fileSystemAccessService.IsSupportedAsync();
            _clipBoardApiSupported = await _asyncClipboardService.IsSupportedAsync();
            _sharedApiSupported = await _shareService.IsSupportedAsync();
            var isFileHandlingSupported = await _fileHandlingService.IsSupportedAsync();
            if (isFileHandlingSupported)
            {
                await _fileHandlingService.SetConsumerAsync(async (p) =>
                {
                    Console.WriteLine("File handle activated");
                    foreach (var fileSystemHandle in p.Files)
                    {
                        Console.WriteLine("File handle has files");
                        if (fileSystemHandle is FileSystemFileHandle fileSystemFileHandle)
                        {
                            var file = await fileSystemFileHandle.GetFileAsync();
                            await _imageService.OpenFileAccessAsync(file.JSReference);
                            await using var context = await _canvas!.GetContext2DAsync();
                            await context.DrawImageAsync("image", 0, 0);
                            StateHasChanged();
                        }
                    }
                });
            }
            await base.OnInitializedAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    await using var context = await _canvas!.GetContext2DAsync();
                    await context.FillStyleAsync("white");
                    await context.FillRectAsync(0, 0, 600, 480);
                    await context.FillStyleAsync("black");

                    _selfReference = DotNetObjectReference.Create(this);
                    if (_module == null)
                    {
                        _module = await JS.InvokeAsync<IJSObjectReference>("import", "./Pages/Index.razor.js");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            await base.OnAfterRenderAsync(firstRender);
        }

        #region Private
        private async Task InternalPointerUp()
        {
            _previousPoint = null;
            await UpdateBage();
        }

        private async void OnPointerDown(PointerEventArgs args)
        {
            if (_module != null && _canvas!.AdditionalAttributes.TryGetValue("id", out var id))
            {
                await _module.InvokeVoidAsync("registerEvents", id, _selfReference);
            }

            _previousPoint = new Point
            {
                X = (int)Math.Floor(args.OffsetX),
                Y = (int)Math.Floor(args.OffsetY)
            };
        }

        private async Task OnPointerMove(PointerEventArgs args)
        {
            if (_previousPoint != null)
            {
                var currentPoint = new Point
                {
                    X = (int)Math.Floor(args.OffsetX),
                    Y = (int)Math.Floor(args.OffsetY)
                };

                var points = _paintService.BrensenhamLine(_previousPoint.Value, currentPoint);
                await using var context = await _canvas!.GetContext2DAsync();
                foreach (var point in points)
                {
                    await context.FillRectAsync(point.X, point.Y, 2, 2);
                }

                _previousPoint = currentPoint;
            }
        }

        private async Task OpenFile(InputFileChangeEventArgs args)
        {
            await using var context = await _canvas!.GetContext2DAsync();
            await _imageService.OpenAsync(args.File.OpenReadStream(1024 * 15 * 1000));
            await context.DrawImageAsync("image", 0, 0);
        }

        private async Task OpenLocalFile()
        {
            try
            {
                var fileHandles = await _fileSystemAccessService.ShowOpenFilePickerAsync(_openFilePickerOptions);
                _fileHandle = fileHandles.Single();
            }
            catch (JSException ex)
            {
                // Handle Exception or cancelation of File Access prompt
                Console.WriteLine(ex);
            }
            finally
            {
                if (_fileHandle is not null)
                {
                    var file = await _fileHandle.GetFileAsync();
                    await _imageService.OpenFileAccessAsync(file.JSReference);
                    await using var context = await _canvas!.GetContext2DAsync();
                    await context.DrawImageAsync("image", 0, 0);
                }
            }
        }

        private async Task DownloadFile()
        {
            await _imageService.DownloadAsync(await _canvas!.ToDataURLAsync());
        }

        private async Task SaveFileLocal()
        {
            try
            {
                if (_fileHandle == null)
                {
                    _fileHandle = await _fileSystemAccessService.ShowSaveFilePickerAsync(_savePickerOptions);
                }

                var writeable = await _fileHandle.CreateWritableAsync();
                var test = await _imageService.GetImageDataAsync("paint-canvas");
                await writeable.WriteAsync(test);
                await writeable.CloseAsync();

                await _fileHandle.JSReference.DisposeAsync();
                _fileHandle = null;
            }
            catch(Exception)
            {
                Console.WriteLine("Save file failed");
            }
            finally
            {
                _fileHandle = null;
                await UpdateBage(true);
            }
        }

        private async void Copy()
        {
            // TODO: Discuss module retrieval
            var imagePromise = _asyncClipboardService.GetObjectReference(_module!, "getCanvasBlob", "paint-canvas");
            var clipboardItem = new ClipboardItem(new Dictionary<string, IJSObjectReference>
            {
                { "image/png", imagePromise }
            });
            await _asyncClipboardService.WriteAsync(new[] { clipboardItem });
        }

        private async Task Paste()
        {
            var clipboardItems = await _asyncClipboardService.ReadAsync();
            var pngItem = clipboardItems.FirstOrDefault(c => c.Types.Contains("image/png"));
            if (pngItem is not null)
            {
                var blob = await pngItem.GetTypeAsync("image/png");
                await _imageService.OpenFileAccessAsync(blob);
                await using var context = await _canvas!.GetContext2DAsync();
                await context.DrawImageAsync("image", 0, 0);
            }
        }

        private async Task Share()
        {
            // TODO: Should reuse blob from copy.
            var fileReference = await _imageService.GenerateFileReferenceAsync(await _canvas!.ToDataURLAsync());
            await _shareService.ShareAsync(new WebShareDataModel
            {
                Files = new[] { fileReference }
            });
        }

        private async Task ResetCanvas()
        {
            await using var context = await _canvas!.GetContext2DAsync();
            await context.FillStyleAsync("white");
            await context.FillRectAsync(0, 0, 600, 480);
            await context.FillStyleAsync("black");
            await UpdateBage(true);
        }

        private async void OnColorChange(ChangeEventArgs args)
        {
            await using var context = await _canvas!.GetContext2DAsync();
            await context.FillStyleAsync(args.Value?.ToString());
        }

        #endregion
    }
}