using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoBackend.Model.Auth;
using TodoBackend.Model.Enums;
using TodoBackend.Repository;

namespace TodoBackend.Controllers;

[Route("v1/api/[controller]")]
public class UserController(UserRepository repository, ILogger<UserController> logger) : ControllerBase
{
    
    [HttpPut("update-user")]
    [Authorize(Policy = AppConstants.CanEditOwnProfile)]
    public async Task<IActionResult> UpdateUser([FromBody]UpdateRequest updateRequest)
    {
        try 
        {
            if (updateRequest == null)
            {
                return BadRequest(new { Error = "Invalid request data." });
            }
            
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { Error = "Invalid user token." });

            
            var user = await repository.GetUserByUserId(userId.Value);
            if (user == null)
            {
                return NotFound(new { Error = "User not found." });
            }
            
            user.UserName = updateRequest.UserName ?? user.UserName;
            user.FirstName = updateRequest.FirstName ?? user.FirstName;
            user.LastName = updateRequest.LastName ?? user.LastName;
            user.PhoneNumber = updateRequest.PhoneNumber ?? user.PhoneNumber;
            user.UpdatedAt = DateTime.UtcNow;
            
            
            await repository.UpdateUser(user);
            return Ok(new { Message = "Profile updated successfully." });
            
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating user profile");
            return StatusCode(500, new { Error = "An error occurred while updating the user profile." });
        }
    }
    
    [HttpPut("update-user/{userId}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> UpdateUserById(int userId, [FromBody] AdminUpdateRequest updateRequest)
    {
        try
        {
            if (updateRequest == null)
                return BadRequest(new { Error = "Invalid request data." });
            
            var user = await repository.GetUserByUserId(userId);
            if(user == null)
                return NotFound(new { Error = "User not found." });
            
            user.UserName = updateRequest.UserName ?? user.UserName;
            user.FirstName = updateRequest.FirstName ?? user.FirstName;
            user.LastName = updateRequest.LastName ?? user.LastName;
            user.PhoneNumber = updateRequest.PhoneNumber ?? user.PhoneNumber;
            user.Role = updateRequest.Role ?? user.Role;
            user.UpdatedAt = DateTime.UtcNow;   
            
            await repository.UpdateUser(user);
            return Ok(new { Message = "User updated successfully." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating user profile by admin");
            return StatusCode(500, new { Error = "An error occurred while updating the user." });
        }
    }
    
    
    [HttpPut("update-profile-image")]
    [Authorize(Policy = AppConstants.CanEditOwnProfile)]
    public async Task<IActionResult> UpdateProfileImage(IFormFile file)
    {
        try 
        {
            return await HandleProfileImageUpload(file);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating profile image");
            return StatusCode(500, new { Error = "An error occurred while updating the profile image.." });
        }
    }

    private async Task<IActionResult> HandleProfileImageUpload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { Error = "No file uploaded." });
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/gif" };

        var extension = Path.GetExtension(file.FileName)
            .ToLowerInvariant()
            .Replace("..", string.Empty);

        if (!allowedExtensions.Contains(extension))
            return BadRequest(new { Error = "Invalid file type. Only JPG, PNG, and GIF images are allowed." });

        if (!allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
            return BadRequest(new { Error = "Invalid file content type." });

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { Error = "File size exceeds 5MB limit." });
        
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { Error = "Invalid user token." });

        
        var user = await repository.GetUserByUserId(userId.Value);
        if (user == null)
        {
            return NotFound(new { Error = "User not found." });
        }

        var uploadsFolder = GetProfileImageFolder();
        
        Directory.CreateDirectory(uploadsFolder);
        if (!DeleteOldProfileImage(user.ProfilePictureUrl))
        {
            logger.LogWarning("Old profile image could not be deleted for user {UserId}", userId);
        }
        
        
        var uniqueFileName = $"{userId}_{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var fileUrl = $"/{AppConstants.UploadsFolderName}/{AppConstants.ProfileImagesFolderName}/{uniqueFileName}";
        user.ProfilePictureUrl = fileUrl;
        user.UpdatedAt = DateTime.UtcNow;
        await repository.UpdateUser(user);

        return Ok(new { Message = "Profile Image updated successfully." });
        
    }
    
    private bool DeleteOldProfileImage(string? profilePictureUrl)
    {
        if (string.IsNullOrEmpty(profilePictureUrl))
            return true;

        var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), 
            AppConstants.WwwRootFolderName, 
            profilePictureUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (!System.IO.File.Exists(oldFilePath)) return true;
        try
        {
            System.IO.File.Delete(oldFilePath); return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting old profile image");
            return false;
        }
    }
    
    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(AppConstants.UserId);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
    
    private string GetProfileImageFolder() =>
        Path.Combine(
            Directory.GetCurrentDirectory(),
            AppConstants.WwwRootFolderName,
            AppConstants.UploadsFolderName,
            AppConstants.ProfileImagesFolderName
        );

}