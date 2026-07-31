CREATE TABLE [dbo].[Clipping] (
    [Id]            NVARCHAR (14)   NOT NULL,
    [Title]         NVARCHAR (1000) NOT NULL,
    [Author]        NVARCHAR (1000) NULL,
    [Type]          NVARCHAR (20)   NOT NULL,
    [Page]          INT             NULL,
    [StartLocation] INT             NULL,
    [EndLocation]   INT             NULL,
    [AddedOn]       DATETIME2 (7)   NULL,
    [Text]          NVARCHAR (MAX)  NULL,
    CONSTRAINT [PK_Clippings] PRIMARY KEY CLUSTERED ([Id] ASC)
);

