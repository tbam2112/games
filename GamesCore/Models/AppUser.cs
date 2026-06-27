using System;
using System.Collections.Generic;

namespace GamesCore.Models
{
    public class AppUser
    {
        public int Id { get; set; }
        
        public string Username { get; set; }
        
        public string Email { get; set; }
        
        public string PasswordHash { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime? LastLoginAt { get; set; }
        
        public bool IsActive { get; set; }
        
        public string DisplayName { get; set; }
        
    }
}
