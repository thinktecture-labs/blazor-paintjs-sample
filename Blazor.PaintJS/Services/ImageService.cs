using Microsoft.JSInterop;

namespace Blazor.PaintJS.Services
{
    public class ImageService : IAsyncDisposable
    {
        protected readonly Lazy<Task<IJSObjectReference>> _moduleTask;

        public ImageService(IJSRuntime jsRuntime)
        {
            _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
               "import", $"./js/{GetType().Name}.js").AsTask());
        }

        public async Task OpenAsync(Stream stream)
        {
            var module = await _moduleTask.Value;
            var currentStream = new DotNetStreamReference(stream);
            await module.InvokeVoidAsync("createImage", currentStream);
        }

        public async Task OpenFileAccessAsync(IJSObjectReference reference)
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("openNewFile", reference);
        }

        public async Task DownloadAsync(string dataUrl)
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("downloadImage", dataUrl);
        }
        
        public async Task<byte[]> GetImageDataAsync(string canvasRefId)
        {
            var module = await _moduleTask.Value;
            return await module.InvokeAsync<byte[]>("getCanvasImageData", canvasRefId);
        }

        public async Task<IJSObjectReference> GenerateFileReferenceAsync(string dataUrl)
        {
            var module = await _moduleTask.Value;
            return await module.InvokeAsync<IJSObjectReference>("generateFile", dataUrl);
        }

        public async ValueTask DisposeAsync()
        {
            if (_moduleTask.IsValueCreated)
            {
                var module = await _moduleTask.Value;
                await module.DisposeAsync();
            }
        }
    }
}
