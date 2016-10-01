CREATE DATABASE [JavBt]

USE [JavBt]

CREATE TABLE [BTDBInfo](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[MovieNo] [nvarchar](100) NOT NULL,
	[TitleName] [nvarchar](400) NULL,
	[MagnetLink] [nvarchar](2000) NULL,
	[ThunderLink] [nvarchar](2000) NULL,
	[FileSize] [nvarchar](50) NULL,
	[FileNum] [nvarchar](20) NULL,
	[FileCreateTime] [nvarchar](100) NULL,
	[FileHot] [nvarchar](20) NULL,
 CONSTRAINT [PK_BTDBInfo] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

CREATE TABLE [MovieDetailList](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[MovieId] [nvarchar](50) NOT NULL,
	[MovieNo] [nvarchar](100) NULL,
	[Title] [nvarchar](400) NOT NULL,
	[DetailHref] [nvarchar](400) NOT NULL,
	[Player] [nvarchar](400) NULL,
	[Director] [nvarchar](100) NULL,
	[Producer] [nvarchar](100) NULL,
	[Publisher] [nvarchar](100) NULL,
	[Series] [nvarchar](100) NULL,
	[Category] [nvarchar](400) NULL,
	[PublishDate] [datetime] NULL,
	[HapenDate] [datetime] NOT NULL DEFAULT (getdate()),
	[MovieTime] [nvarchar](50) NULL,
	[CoverHref] [nvarchar](400) NULL,
	[thumbnailHref] [nvarchar](400) NULL,
	[MovieGallery] [nvarchar](max) NULL,
	[thumbnailImg] [image] NULL,
	[CoverImg] [image] NULL,
 CONSTRAINT [PK_Text] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]