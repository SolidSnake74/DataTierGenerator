select TABLE_CATALOG,
	TABLE_SCHEMA,
	TABLE_NAME,
	TABLE_TYPE
from INFORMATION_SCHEMA.TABLES
where TABLE_TYPE = 'BASE TABLE'
	and (TABLE_NAME != 'dtProperties' and TABLE_NAME != 'sysdiagrams')
	and TABLE_CATALOG = '#DatabaseName#'
order by TABLE_NAME
