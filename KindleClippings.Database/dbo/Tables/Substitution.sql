CREATE TABLE [dbo].[Substitution] (
    [ClippingId] NVARCHAR (140)  NOT NULL,
    [NewTitle]   NVARCHAR (1000) NOT NULL,
    CONSTRAINT [PK_Substitution] PRIMARY KEY CLUSTERED ([ClippingId] ASC)
);

