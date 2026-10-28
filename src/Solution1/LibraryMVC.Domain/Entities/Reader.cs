namespace LibraryMVC.Domain.Entities;

public class Reader : Entity, IAggregateRoot
{
    public Reader(string fullName, string faculty, string department, string position)
    {
        FullName = fullName;
        Faculty = faculty;
        Department = department;
        Position = position;
        RegisteredAt = DateTime.UtcNow;
    }

    public string FullName { get; private set; }
    public string Faculty {  get; private set; }
    public string Department { get; private set; }
    public string Position { get; private set; }
    public DateTime RegisteredAt { get; private set; }

    public void Update(string fullName, string faculty, string department, string position)
    {
        FullName = fullName;
        Faculty = faculty;
        Department = department;
        Position = position;
    }
}
