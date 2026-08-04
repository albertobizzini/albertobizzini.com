CREATE TABLE [dbo].[OldClipping] (
    [Id]            NVARCHAR (14)  NOT NULL,
    [Title]         NVARCHAR (255) NOT NULL,
    [Author]        NVARCHAR (255) NULL,
    [Type]          NVARCHAR (20)  CONSTRAINT [DF_OldClipping_Type] DEFAULT ('Highlight') NOT NULL,
    [Page]          INT            NULL,
    [StartLocation] INT            NULL,
    [EndLocation]   INT            NULL,
    [AddedOn]       DATE           NULL,
    [Text]          NVARCHAR (MAX) NULL,
    [ImportedAt]    DATETIME2 (7)  CONSTRAINT [DF_OldClipping_ImportedAt] DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_OldClipping] PRIMARY KEY CLUSTERED ([Id] ASC)
);

