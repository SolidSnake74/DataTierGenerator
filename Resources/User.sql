use #DatabaseName#

/******************************************************************************************
Create the #UserName# login.
******************************************************************************************/
if not exists(select * from master..syslogins where name = '#UserName#')
	exec sp_addlogin '#UserName#', '', '#DatabaseName#'
go

/******************************************************************************************
Grant the #UserName# login access to the #DatabaseName# database.
******************************************************************************************/
if not exists (select * from [dbo].sysusers where name = N'#UserName#' and uid < 16382)
	exec sp_grantdbaccess N'#UserName#', N'#UserName#'
go
