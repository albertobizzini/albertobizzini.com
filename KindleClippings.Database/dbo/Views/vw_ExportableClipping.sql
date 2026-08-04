CREATE VIEW [dbo].[vw_ExportableClipping] AS
SELECT 
	c.Id,
	c.Title,
	c.Author,
	c.Type,
	c.Page,
	c.StartLocation,
	c.EndLocation,
	c.AddedOn,
	c.Text,
	c.ImportedAt
FROM
	dbo.Clipping AS c
WHERE
	c.ID NOT IN (SELECT Id FROM dbo.IgnorableClipping)
AND c.ID NOT IN (SELECT Id FROM dbo.vw_OverlappingClipping)

UNION ALL

SELECT 
	o.Id,
	o.Title,
	o.Author,
	o.Type,
	o.Page,
	o.StartLocation,
	o.EndLocation,
	o.AddedOn,
	o.Text,
	o.ImportedAt
FROM
	dbo.OldClipping AS o