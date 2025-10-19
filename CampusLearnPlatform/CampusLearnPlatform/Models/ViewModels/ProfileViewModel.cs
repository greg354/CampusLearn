namespace CampusLearnPlatform.Models.ViewModels
{
    public class ProfileViewModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = "";
        public string Email { get; set; } = "";

        public string? ProfileInfo { get; set; }
        public string? ProfilePictureUrl { get; set; }

        public bool IsMe { get; set; }

        // For uploads
        public IFormFile? ProfileImage { get; set; }
    }
}
