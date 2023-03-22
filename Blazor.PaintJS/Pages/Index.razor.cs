using Blazor.PaintJS.Services;
using Excubo.Blazor.Canvas;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Drawing;
using KristofferStrube.Blazor.FileSystemAccess;
using KristofferStrube.Blazor.FileSystem;

namespace Blazor.PaintJS.Pages
{
    public partial class Index
    {
        [Inject] public IJSRuntime JS { get; set; } = default!;
        [Inject] private PaintService _paintService { get; set; } = default!;
        [Inject] private ImageService _imageService { get; set; } = default!;

        private IJSObjectReference? _module;
        private DotNetObjectReference<Index>? _selfReference;

        #region FileHandle Properties
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
        #endregion

        #region SupportedProps
        private bool _fileSystemAccessSupported = false;
        private bool _clipBoardApiSupported = false;
        private bool _sharedApiSupported = false;
        private bool _badgeApiSupported = false;
        #endregion

        private Canvas? _canvas;
        private Point? _previousPoint;
        private int _hasChanges = 0;

        // Method which is JSInvokable must be public
        [JSInvokable]
        public void OnPointerUp()
        {
            _previousPoint = null;
        }

        [JSInvokable]
        public async Task DrawImageAsync()
        {
            await using var context = await _canvas!.GetContext2DAsync();
            await context.DrawImageAsync("image", 0, 0);
        }

        protected override async Task OnInitializedAsync()
        {
            // OVERALL: Check supported state with nuget packages

            // LEKTION 4: FileHandling Support | Get all FileSystemFileHandle

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

        // LEKTION 1 - 1 Async Clipboard API | Copy | "getCanvasBlob", "paint-canvas"
        private async void Copy()
        {
        }

        // LEKTION 1 - 2 Async Clipboard API | Paste
        private async Task Paste()
        {
        }

        // LEKTION 2: WebShareAPI
        private async Task Share()
        {
        }

        // LEKTION 3 - 1: Open File with FilesystemAccess
        private async Task OpenLocalFile()
        {
        }

        // LEKTION 3 - 2: Save File with FilesystemAccess
        private async Task SaveFileLocal()
        {
        }

        //LEKTION 5: Update Badge with API
        private async Task UpdateBage(bool reset = false)
        {
        }

        #region Helper Methods
        private async Task OpenFile(InputFileChangeEventArgs args)
        {
            await using var context = await _canvas!.GetContext2DAsync();
            await _imageService.OpenAsync(args.File.OpenReadStream(1024 * 15 * 1000));
            await context.DrawImageAsync("image", 0, 0);
        }
        private async Task DownloadFile()
        {
            await _imageService.DownloadAsync(await _canvas!.ToDataURLAsync());
        }
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