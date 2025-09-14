using CampusLearnPlatform.Models.Users;

namespace CampusLearnPlatform.Models.Learning
{
    public class Module
    {
        public int Id { get; set; }
        public string ModuleCode { get; set; }
        public string ModuleName { get; set; }
        public string Description { get; set; }
        public int Credits { get; set; }
        public int AcademicYear { get; set; }
        public string Semester { get; set; }
        public bool IsActive { get; set; }

        public virtual ICollection<Topic> Topics { get; set; }
        public virtual ICollection<Tutor> Tutors { get; set; }
        public virtual ICollection<Student> Students { get; set; }


        public Module()
        {
            IsActive = true;
            Topics = new List<Topic>();
            Tutors = new List<Tutor>();
            Students = new List<Student>();
        }

        public Module(string moduleCode, string moduleName, int credits, int academicYear) : this()
        {
            ModuleCode = moduleCode;
            ModuleName = moduleName;
            Credits = credits;
            AcademicYear = academicYear;
        }

  
        public void AddTutor(Tutor tutor) { }
        public void RemoveTutor(int tutorId) { }
        public List<Topic> GetActiveTopics()
        {
            return new List<Topic>();
        }
        public int GetEnrolledStudentCount()
        {
            return Students?.Count ?? 0;
        }
        public bool HasTutor(int tutorId)
        {
            return true;
        }
        public void UpdateDetails(string description, int credits)
        {
            Description = description;
            Credits = credits;
        }
        public void DeactivateModule()
        {
            IsActive = false;
        }
    }
}
