use MyKindergarden;
GO

create procedure UpdateVisitNoteDates
as
	begin
	declare @lastUpdate date;
	set @lastUpdate = (select top 1 LastUpdate from VisitDateSupportTable);
	if(GETDATE() > @lastUpdate)
		begin
		update VisitNote set Visited = 0 
			where (select VisitDate.Date from VisitDate where Id = VisitNote.VisitDate_Id)
				between DATEADD(DAY,1,@lastUpdate) and GETDATE();
		update VisitDateSupportTable set LastUpdate = GETDATE();
		end;
	end;
GO

create function Is_workday(@d date)
	returns bit
	as
	begin
	if(DATEPART(dw, @d) in (1, 7)) 
		return 0;
	return 1;
	end;
GO
create procedure Insert_date
	@d date
as
	if @d not in (select VisitDate.Date from VisitDate)
		insert VisitDate(Date, IsVisitDate) values (@d, dbo.Is_workday(@d));
GO
create procedure Insert_dates 
	@d1 date,
	@d2 date
as
	declare @buff date;
	set @buff = @d1;
	while @buff <= @d2
	begin
		execute Insert_date @buff;
		SET @buff=DATEADD(DAY,1,@buff);
	end;
GO
create function LastVisitDate()
	returns date
	as
	begin
		return (select top 1 Date from VisitDate order by Date desc)
	end;
GO
create function SelectDate (@d date, @alt date) 
	returns date
	as
	begin
		return (case when @d is null then @alt else @d end)
	end;
GO

create trigger ClientUsrPwdConstraint ON Client after INSERT, UPDATE 
AS
	IF EXISTS(select * from inserted where Len(Username) < 6 or Len(inserted.Password) < 6)
		BEGIN  
		RAISERROR ('Length of Username and Password must be more than 6 characters', 16, 1);  
		ROLLBACK TRANSACTION;  
		RETURN   
		END;
GO

create trigger ClientTypeChangeBlock ON Client after UPDATE 
AS
	IF EXISTS(select inserted.* from inserted join deleted on inserted.Id = deleted.Id and inserted.ClientType_Id != deleted.ClientType_Id)
		BEGIN  
			RAISERROR ('ClientType can`t be changed', 16, 1);  
			ROLLBACK TRANSACTION;  
			RETURN   
		END;
GO

create trigger TeacherGroupAdminBlock ON TeacherGroup after INSERT, UPDATE  
AS
	IF EXISTS(select inserted.* from inserted where inserted.Teacher_Id in (select Id from Client where ClientType_Id = 0))
		BEGIN  
			RAISERROR ('Admin can`t contain groups', 16, 1);  
			ROLLBACK TRANSACTION;  
			RETURN   
		END;
GO

create trigger VisitDateUpdateBlock on VisitDate after UPDATE
as
	if exists(select inserted.* from inserted join deleted on deleted.Id = inserted.Id and deleted.Date != inserted.Date)
		BEGIN
			RAISERROR ('Date can`t be updated', 16, 1);  
			ROLLBACK TRANSACTION;
		END;
	RETURN
GO

create trigger VisitDateUpdate on VisitDate after UPDATE
as
	update VisitNote
		set Visited = null, Additional = null
		where VisitNote.VisitDate_Id in
		(
			select inserted.Id from inserted join deleted on inserted.Id = deleted.Id 
				and inserted.IsVisitDate != deleted.IsVisitDate
				where inserted.IsVisitDate = 0 or inserted.Date > GETDATE()
		)
	update VisitNote
		set Visited = 0
		where VisitNote.VisitDate_Id in 
		(
			select inserted.Id from inserted join deleted on inserted.Id = deleted.Id and inserted.IsVisitDate != deleted.IsVisitDate
				where inserted.IsVisitDate = 1 and inserted.Date <= GETDATE()
		)
GO
create trigger VisitDateInsert on VisitDate after INSERT
as
	insert into VisitNote(Child_Id, VisitDate_Id, Visited)
		select chld.Id, inserted.Id, 
				(case when (inserted.Date > GETDATE() or inserted.IsVisitDate = 0) then null else 0 end)
			from (select * from inserted) as inserted
			cross join Child chld  
				where inserted.Date between chld.VisitStart and dbo.SelectDate(chld.VisitEnd, dbo.LastVisitDate());
GO


create trigger VisitNoteUpdateBlock ON VisitNote after UPDATE 
AS
	if exists(select inserted.* from inserted join deleted on inserted.Id = deleted.Id 
				and (inserted.VisitDate_Id != deleted.VisitDate_Id or inserted.Child_Id != deleted.Child_Id))
	BEGIN  
		RAISERROR ('Attributes Child and Date can`t be updated', 16, 1);  
		ROLLBACK TRANSACTION;  
		RETURN   
	END;
GO

create trigger VisitStatusUpdateBlock ON VisitNote after UPDATE 
AS
	if exists(select inserted.* from inserted 
				join VisitDate on inserted.VisitDate_Id = VisitDate.Id 
					and VisitDate.IsVisitDate = 0
					and inserted.Visited is null)
	BEGIN  
		RAISERROR ('Visit status must be nullable when VisitDate.IsVisitDate = false', 16, 1);  
		ROLLBACK TRANSACTION;  
		RETURN   
	END;
GO


create trigger WrongDatesBlock ON Child after INSERT, Update
AS
	if exists(select * from inserted where inserted.VisitStart > inserted.VisitEnd or inserted.VisitStart < VisitStart.BirthDate)
	BEGIN  
		RAISERROR ('Wrong dates.', 16, 1);  
		ROLLBACK TRANSACTION;  
		RETURN   
	END;
GO
create trigger UpdateVisitNoteAddition ON Child after UPDATE 
AS
	insert into VisitNote(Child_Id, VisitDate_Id, Visited)
		select inserted.Id, vdate.Id, (case when (vdate.Date > GETDATE() or vdate.IsVisitDate = 0) then null else 0 end) from 
			(select inserted.* from inserted join deleted 
				on deleted.Id = inserted.Id
				and (deleted.VisitStart != inserted.VisitStart or deleted.VisitEnd != inserted.VisitEnd)) inserted
		cross join VisitDate as vdate 
			where vdate.Date between inserted.VisitStart and dbo.SelectDate(inserted.VisitEnd, dbo.LastVisitDate())
			and not exists(select * from VisitNote where VisitNote.VisitDate_Id = vdate.Id and VisitNote.Child_Id = inserted.Id);
GO
create trigger InsertVisitNoteAddition ON Child after INSERT
AS
	insert into VisitNote(Child_Id, VisitDate_Id, Visited)
		select inserted.Id, vdate.Id, (case when (vdate.Date > GETDATE() or vdate.IsVisitDate = 0) then null else 0 end) 
		from inserted
		cross join VisitDate as vdate 
			where vdate.Date between inserted.VisitStart and dbo.SelectDate(inserted.VisitEnd, dbo.LastVisitDate());
GO


