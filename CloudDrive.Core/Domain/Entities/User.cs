namespace CloudDrive.Core.Domain.Entities
{
    public class User
    {
        public Guid UserId { get; set; }
        public string Email {  get; set; }
        public string Password { get; set; } 
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set;}
    }
}
