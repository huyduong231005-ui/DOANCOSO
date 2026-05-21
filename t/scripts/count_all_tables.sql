SET NOCOUNT ON;
DECLARE @sql NVARCHAR(MAX) = '';
SELECT @sql = @sql + 'UNION ALL SELECT ''' + name + ''' AS Bang, COUNT(*) AS SoRecord FROM ' + QUOTENAME(name) + ' '
FROM sys.tables WHERE name NOT LIKE '__%';
SET @sql = STUFF(@sql, 1, 10, '') + ' ORDER BY Bang';
EXEC sp_executesql @sql;
