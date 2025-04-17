using CloudDrive.Core.DTO;
using Entities = CloudDrive.Core.Domain.Entities;

namespace CloudDrive.Core.Mappers
{
    public static class UserMapper
    {
        public static UserDTO ToDto(this Entities.User user)
        {
            return new UserDTO
            {
                UserId = user.UserId,
                Email = user.Email,
                CreatedDate = user.CreatedDate,
                ModifiedDate = user.ModifiedDate
            };
        }
    }
}
