using System;
using System.Collections.Generic;
using System.Text;

namespace LocalRag.Application.DTOs
{
    public class AgentResponseDto
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public List<string> ToolsUsed { get; set; } = [];
    }
}
