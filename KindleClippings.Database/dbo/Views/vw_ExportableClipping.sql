CREATE VIEW dbo.vw_ExportableClipping AS
SELECT 
	c.*
FROM
	dbo.Clipping AS c
WHERE
	c.ID NOT IN (SELECT Id FROM dbo.IgnorableClipping)
AND c.ID NOT IN (SELECT Id FROM dbo.vw_OverlappingClipping)