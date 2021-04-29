create database MyKindergarden;
GO
use MyKindergarden;

create table ClientType
(
	Id int primary key,
	TypeName nvarchar(20) not null unique
);
insert into ClientType values
(0, N'admin'),
(1, N'teacher');

create table GroupType
(
	Id int primary key,
	TypeName nvarchar(20) not null unique
);
insert into GroupType values
(1, N'Первая младшая'),
(2, N'Вторая младшая'),
(3, N'Средняя'),
(4, N'Старшая');

create table Client 
(
	Id int identity(1,1) primary key,
	Username nvarchar(30) not null unique,
	"Password" nvarchar(16) not null,
	FirstName nvarchar(30) not null,
	LastName nvarchar(30) not null,
	MiddleName nvarchar(30) not null,
	IsActive bit not null default 1,
	ClientType_Id int not null default 1,
	foreign key (ClientType_Id) references ClientType(Id)
);
insert into Client(Username, Password, FirstName, LastName, MiddleName, IsActive, ClientType_Id) values
	(N'admin', N'admin', N'Роман', N'Гупало', N'Валерьевич', 1, 0),
	(N'teacher', N'teacher', N'Гупало', N'Роман', N'Валерьевич', 1, 1);

create table KinderGroup
(
	Id int identity(1,1) primary key,
	GroupName nvarchar(30) not null unique,
	GroupType_Id int not null default 0,
	IsActive bit not null default 1,
	foreign key (GroupType_Id) references GroupType (Id)
);


create table Child
(
	Id int identity(1,1) primary key,
	FirstName nvarchar(30) not null,
	LastName nvarchar(30) not null,
	MiddleName nvarchar(30) not null,
	BirthDate date not null default GETDATE(),
	KinderGroup_Id int null,
	VisitStart date not null,
	VisitEnd date null,
	foreign key (KinderGroup_Id) references KinderGroup (Id) on delete set null
);

create table TeacherGroup
(
	Teacher_Id int not null,
	KinderGroup_Id int not null,
	foreign key (Teacher_Id) references Client (Id) on delete cascade,
	foreign key (KinderGroup_Id) references KinderGroup (Id) on delete cascade,
	constraint PK_TeacherGroup primary key clustered (Teacher_Id, KinderGroup_Id)
);

create table VisitDate
(
	Id int identity(1,1) primary key,
	"Date" date not null unique,
	IsVisitDate bit not null default 1,
);

create table VisitNote
(
	Id int identity(1,1) primary key,
	Child_Id int not null,
	VisitDate_Id int not null,
	Visited bit default null,
	Additional nvarchar(100) null,
	foreign key (Child_Id) references Child (Id) on delete cascade,
	foreign key (VisitDate_Id) references VisitDate (Id) on delete cascade,
	CONSTRAINT uc_ChildDate UNIQUE (Child_Id, VisitDate_Id)

);

create table VisitDateSupportTable
(
	Id bit primary key,
	LastUpdate date null
);

insert VisitDateSupportTable values (0,GETDATE());