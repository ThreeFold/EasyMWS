﻿using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MountainWarehouse.EasyMWS.Data;

[assembly: InternalsVisibleTo("EasyMWS.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
namespace MountainWarehouse.EasyMWS.Repositories
{
    internal interface IReportRequestCallbackRepo
    {
	    void Create(ReportRequestCallback callback);
	    Task CreateAsync(ReportRequestCallback callback);
	    void Update(ReportRequestCallback callback);
	    void Delete(int id);
	    IQueryable<ReportRequestCallback> GetAll();
		void SaveChanges();
	    Task SaveChangesAsync();

    }
}
