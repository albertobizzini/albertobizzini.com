using AlbertoBizzini.Web.Shared;
using MudBlazor;

namespace AlbertoBizzini.Web.Services
{
    public class SlideshowService
    {
        private readonly IDialogService _dialogService;

        public SlideshowService(IDialogService dialogService)
        {
            _dialogService = dialogService;
        }

        public async Task ShowAsync(
            IEnumerable<string> imageUrls,
            string title,
            bool autoCycle = true,
            int cycleSeconds = 5)
        {
            var parameters = new DialogParameters<ImageSlideshowDialog>
        {
            { x => x.ImageUrls, imageUrls.ToList() },
            { x => x.AutoCycle, autoCycle },
            { x => x.CycleTime, TimeSpan.FromSeconds(cycleSeconds) }
        };

            var options = new DialogOptions
            {
                MaxWidth = MaxWidth.Large,
                FullWidth = true,
                CloseButton = true,
                BackdropClick = true
            };

            await _dialogService.ShowAsync<ImageSlideshowDialog>(
                title,
                parameters,
                options);
        }
    }
}
