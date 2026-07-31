CREATE VIEW [dbo].[vw_OverlappingClipping] AS
SELECT 
    c2.Id,
    c2.Text,
    c.Id AS ContainedById,
    -- Determina la parte di testo che precede il sotto-testo
    CASE 
        WHEN CHARINDEX(c2.Text, c.Text) > 1 
        THEN '[…] ' 
        ELSE '' 
    END 
    + c2.Text + 
    -- Determina la parte di testo che segue il sotto-testo
    CASE 
        WHEN CHARINDEX(c2.Text, c.Text) + LEN(c2.Text) <= LEN(c.Text) 
        THEN ' […]' 
        ELSE '' 
    END AS VisualContext,
    
    -- Estrae l'intero testo "contenitore" per mostrare cosa c'è intorno
    c.Text AS FullText
FROM   
    dbo.Clipping AS c
    INNER JOIN
    dbo.Clipping AS c2
    ON c2.Id <> c.Id
        AND LEN(c2.Text) < LEN(c.Text)
        AND CHARINDEX(c2.Text, c.Text) > 0 -- Il testo corto è contenuto ovunque nel testo lungo
        AND c2.Title = c.Title
        AND c2.Author = c.Author;