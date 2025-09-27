using System.ComponentModel.DataAnnotations;

namespace CampusLearnPlatform.Models.ViewModels
{
    public class TutorRegisterViewModel
    {
        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@tutor\.belgiumcampus\.ac\.za$",
            ErrorMessage = "Email must be a valid Belgium Campus tutor email (@tutor.belgiumcampus.ac.za)")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "At least one expertise area is required")]
        public List<string> Expertise { get; set; } = new List<string>();

        [Required(ErrorMessage = "Password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[!@#$&*])(?=.*[0-9])(?=.*[a-z]).{8,}$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "You must accept the terms and conditions")]
        public bool AcceptTerms { get; set; }

        // List of available expertise areas based on Belgium Campus modules
        public static List<string> ExpertiseAreas => new List<string>
        {
            "Academic Writing",
            "Business Communication",
            "Business Management",
            "Business Intelligence",
            "Cloud-Native Application Architecture",
            "Cloud-Native Application Programming",
            "Computer Architecture",
            "Data Analytics",
            "Data Science",
            "Data Warehousing",
            "Database Administration",
            "Database Concepts",
            "Database Development",
            "End User Computing",
            "English Communication",
            "Enterprise Systems",
            "Entrepreneurship",
            "Ethics & IT Law",
            "Information Systems",
            "Innovation and Leadership",
            "Innovation Management",
            "Internet of Things (IoT)",
            "Linear Programming",
            "Machine Learning",
            "Mathematics",
            "Network Development",
            "Problem Solving",
            "Programming (General)",
            "Programming (Advanced)",
            "Programming (C#)",
            "Programming (Java)",
            "Programming (Python)",
            "Project Management",
            "Research Methods",
            "Software Analysis & Design",
            "Software Engineering",
            "Software Testing",
            "Statistics",
            "User Experience Design",
            "Web Programming"
        };
    }
}