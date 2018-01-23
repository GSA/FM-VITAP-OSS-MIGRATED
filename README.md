# VITAP
The Visual Invoice Tracking and Payment (VITAP) application is a front end accounts payable matching system that provides a variety of functionality for GSA Federal Acquisition Service and Public Building Service business lines. VITAP provides data for three of GSA OCFO Web applications: Web Vendors, Invoice Search, and PO Search. It also provides interfaces for the UPPS to flow into Pegasys. 

### Key Functions 
Accounting document matching, validation, exception processing, workflow routing, invoice tracking, obligations processing, payment, accruals document management for unprocessed documents, transaction history, email notifications, report generation, and financial management information.

## Getting Started
These instructions will get you a copy of the project up and running on your local machine for development and testing purposes. See deployment for notes on how to deploy the project on a live system.

### Prerequisites
- Visual Studio 2015
- .NET Framework 4.6.2
- Entity Framework 6.0.0.0
- Official Oracle ODP.NET, Managed Entity Framework Driver
- Kendo.Mvc (Kendo Web Extensions for ASP.NET MVC)
- Elmah.Mvc 2.1.2
- GSA.R7BC.Utility 2.1.0
- Oracle database server

### Installing
1. Download and install the prerequisite software
2. Download VITAP source code or folk this repo
3. Download and create VITAP tables in an Oracle database schema
4. Open VITAP Solution in Visual Studio.  You should see VITAP, VITAP.Data, and VITAP.Library projects in VITAP solution. 
5. Set VITAP as StartUp project if VITAP is not a StartUp project in the Solution. 
6. Build VITAP solution in Visual Studio

## Built With
- C#
- Entity Framework
- Oracle Databse 11g
- LINQ
- Kendo
- JavaScript
- ASP.NET Razor
- JQuery
- Bootstrap
- .NET MVC
- CSS
- HTML

## Authors

**GSA IT - Office of Corporate IT Services**, [Financial Management IT Services (ICSF)](https://github.com/orgs/GSA/teams/corporate-it-services/members)

See also the list of [contributors](https://github.com/GSA/FM-VITAP/graphs/contributors) who participated in this project.
