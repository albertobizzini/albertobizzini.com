CREATE VIEW [dbo].[vw_ExportableClipping] AS
WITH Base AS (
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
)

SELECT
	b.Id,
	COALESCE(s.NewTitle, b.Title) AS Title,
	b.Author,
	b.Type,
	b.Page,
	b.StartLocation,
	b.EndLocation,
	b.AddedOn,
	b.Text,
	b.ImportedAt
FROM 
	Base AS b

	LEFT JOIN
	dbo.Substitution AS s
		ON s.ClippingId = b.Id