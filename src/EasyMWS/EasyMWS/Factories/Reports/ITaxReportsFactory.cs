﻿using MountainWarehouse.EasyMWS.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace MountainWarehouse.EasyMWS.Factories.Reports
{
    public interface ITaxReportsFactory
    {
		/// <summary>
		/// Generate a request object for a MWS report of type : _GET_FLAT_FILE_SALES_TAX_DATA_ <para />
		/// Tab-delimited flat file for tax-enabled US sellers. Content updated daily. This report cannot be requested or scheduled. You must generate the report from the Tax Document Library in Seller Central.<para/>
		/// After the report has been generated, you can download the report using the GetReportList and GetReport operations. For Marketplace and Seller Central sellers.
		/// </summary>
		/// <param name="endDate">Optional argument that can help specify an interval of time for which the report is generated.</param>
		/// <param name="requestedMarketplaces">(NA, EU only) Optional list of marketplaces used when submitting a report request. For more info see MwsMarketplaceGroup class summary.</param>
		/// <param name="startDate">Optional argument that can help specify an interval of time for which the report is generated.</param>
		/// <returns></returns>
		ReportRequestPropertiesContainer SalesTaxReport(DateTime? startDate = null, DateTime? endDate = null, IEnumerable<MwsMarketplace> requestedMarketplaces = null);

		/// <summary>
		/// Generate a request object for a MWS report of type : _SC_VAT_TAX_REPORT_ <para />
		/// Comma-separated flat file report that provides detailed value-added tax (VAT) calculation information for buyer shipments, returns, and refunds.<para/>
		/// This report is only available in the Germany, Spain, Italy, France, and UK marketplaces.
		/// </summary>
		/// <param name="endDate">Optional argument that can help specify an interval of time for which the report is generated.</param>
		/// <param name="requestedMarketplaces">(EU only) Optional list of marketplaces used when submitting a report request. For more info see MwsMarketplaceGroup class summary.</param>
		/// <param name="startDate">Optional argument that can help specify an interval of time for which the report is generated.</param>
		/// <returns></returns>
		ReportRequestPropertiesContainer AmazonVATCalculationReport(DateTime? startDate = null, DateTime? endDate = null, IEnumerable<MwsMarketplace> requestedMarketplaces = null);

		/// <summary>
		/// Generate a request object for a MWS report of type : _GET_VAT_TRANSACTION_DATA_ <para />
		/// Tab-delimited flat file report that provides detailed information for sales, returns, refunds, cross border inbound and cross border fulfillment center transfers.<para/>
		/// This report is only available in the Germany, Spain, Italy, France, and UK marketplaces.
		/// </summary>
		/// <param name="endDate">Optional argument that can help specify an interval of time for which the report is generated.</param>
		/// <param name="requestedMarketplaces">(EU only) Optional list of marketplaces used when submitting a report request. For more info see MwsMarketplaceGroup class summary.</param>
		/// <param name="startDate">Optional argument that can help specify an interval of time for which the report is generated.</param>
		/// <returns></returns>
		ReportRequestPropertiesContainer AmazonVATTransactionsReport(DateTime? startDate = null, DateTime? endDate = null, IEnumerable<MwsMarketplace> requestedMarketplaces = null);

		/// <summary>
		/// Generate a request object for a MWS report of type : _GET_GST_MTR_B2B_CUSTOM_ <para />
		/// Tab-delimited flat file report that provides detailed information about sales, refunds, and cancellations from Amazon Business invoices issued within a date range that you specify.<para/>
		/// This report is only available in the India marketplace.
		/// </summary>
		/// <param name="startDate">Optional argument that can help specify an interval of time for which the report is generated.</param>
		/// <param name="endDate">Optional argument that can help specify an interval of time for which the report is generated.</param>
		/// <param name="requestedMarketplaces"></param>
		/// <returns></returns>
		ReportRequestPropertiesContainer OnDemandGSTMerchantTaxReportB2B(DateTime? startDate = null, DateTime? endDate = null);

		/// <summary>
		/// Generate a request object for a MWS report of type : _GET_GST_MTR_B2C_CUSTOM_ <para />
		/// Tab-delimited flat file report that provides detailed information about sales, refunds, and cancellations from consumer invoices issued within a date range that you specify.<para/>
		/// This report is only available in the India marketplace.
		/// </summary>
		/// <param name="startDate">Optional argument that can help specify an interval of time for which the report is generated.</param>
		/// <param name="endDate">Optional argument that can help specify an interval of time for which the report is generated.</param>
		/// <param name="requestedMarketplaces"></param>
		/// <returns></returns>
		ReportRequestPropertiesContainer OnDemandGSTMerchantTaxReportB2C(DateTime? startDate = null, DateTime? endDate = null);
	}
}
