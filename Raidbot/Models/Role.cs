using System;
using System.Collections.Generic;
using System.Text;

namespace Raidbot.Models
{
	public class Role
	{
        public Role(int spots, string name, string description = "")
        {
            Name = name;
            Spots = spots;
            Description = description;
        }

        public string Name { get; set; }
        public int Spots { get; }
        public string Description { get; set; }
    }
}
