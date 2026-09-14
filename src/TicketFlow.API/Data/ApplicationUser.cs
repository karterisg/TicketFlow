using Microsoft.AspNetCore.Identity;
//using System.Globalization;



public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty; //in identity
    public string Role { get; set; } = "Customer"; //in different storage
}