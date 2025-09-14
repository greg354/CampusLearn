namespace CampusLearnPlatform.Models.Users
{
    public class UserProfile
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string Biography { get; set; }
        public string ProfilePicture { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Address { get; set; }

      
        public int UserId { get; set; }

        public virtual User User { get; set; }

     
        

        public UserProfile(string firstName, string lastName, int userId)
        {
            FirstName = firstName;
            LastName = lastName;
            UserId = userId;
        }

 
        public void UpdatePersonalInfo(string firstName, string lastName, string phone)
        {
            FirstName = firstName;
            LastName = lastName;
            PhoneNumber = phone;
        }
        public void UploadProfilePicture(string imagePath)
        {
            ProfilePicture = imagePath;
        }
        public string GetFullName()
        {
            return $"{FirstName} {LastName}";
        }
        public void UpdateBiography(string newBio)
        {
            Biography = newBio;
        }
    }
}
