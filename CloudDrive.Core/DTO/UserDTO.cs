namespace CloudDrive.Infrastructure.DTO
{
    public class UserDTO
    {
        public Guid UserId { get; set; }
        public string Email { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}
