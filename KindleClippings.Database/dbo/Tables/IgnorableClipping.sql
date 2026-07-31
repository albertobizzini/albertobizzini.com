CREATE TABLE [dbo].[IgnorableClipping] (
    [Id]         VARCHAR (14)   NOT NULL,
    [Motivation] NVARCHAR (100) NOT NULL,
    CONSTRAINT [PK_ClippingToIgnore] PRIMARY KEY CLUSTERED ([Id] ASC)
);

