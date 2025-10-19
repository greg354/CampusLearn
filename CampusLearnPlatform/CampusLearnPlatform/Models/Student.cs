namespace CampusLearnPlatform.Models
{
    public class Student
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = "";
        public string Email { get; set; } = "";

        // NEW
        public string? ProfileInfo { get; set; }
        public string? ProfilePictureUrl { get; set; } // e.g. "/uploads/profiles/abc.jpg"
    }
}
