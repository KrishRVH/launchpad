using Microsoft.AspNetCore.Identity;

namespace Launchpad.Web.Data;

public class ApplicationUser : IdentityUser {
    public string DisplayName { get; set; } = "";
    public string StudioTitle { get; set; } = "";
}

