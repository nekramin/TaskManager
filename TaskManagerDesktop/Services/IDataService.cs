using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManagerDesktop.Services
{
    public class IDataService
    {
        public Task<TaskItem> SaveTaskAsync(TaskItem task)
        {
        }

        public Task<bool> UpdateTaskAsync(TaskItem task)
        {
        }
    }
}
