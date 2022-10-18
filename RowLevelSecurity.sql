-- CLEAN UP

DROP USER ChatApp
DROP LOGIN ChatApp

DROP SECURITY POLICY Security.RoomsFilter

DROP SECURITY POLICY Security.MessagesFilter

DROP FUNCTION security.fn_CheckMessages
DROP FUNCTION security.fn_CheckMatchingCompany

DROP SCHEMA security

-- ChatApp Account


CREATE LOGIN ChatApp 
	WITH PASSWORD = 'Chat123!'
GO

CREATE USER ChatApp
	FOR LOGIN ChatApp
	WITH DEFAULT_SCHEMA = dbo
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA :: dbo TO ChatApp;
GRANT SHOWPLAN TO ChatApp;
GO


-- Security policies

CREATE SCHEMA security;
GO

CREATE FUNCTION security.fn_CheckMatchingCompany(@CompanyId NVARCHAR(10))  
    RETURNS TABLE  
    WITH SCHEMABINDING  
AS  
    RETURN SELECT 1 AS fn_CheckMatchingCompany_result  
    WHERE  
		IS_ROLEMEMBER (N'db_owner') = 1 OR
        CAST(SESSION_CONTEXT(N'CompanyId') AS NVARCHAR(10)) = @CompanyId;
GO

CREATE FUNCTION security.fn_CheckMessages(@RoomId int)  
    RETURNS TABLE  
    WITH SCHEMABINDING  
AS  
    RETURN SELECT 1 AS fn_CheckMessages_result
	FROM dbo.Rooms r
	WHERE r.Id = @RoomId AND 
	EXISTS (SELECT fn_CheckMatchingCompany_result FROM security.fn_CheckMatchingCompany(r.CompanyId))
GO


CREATE SECURITY POLICY Security.RoomsFilter
    ADD FILTER PREDICATE Security.fn_CheckMatchingCompany(CompanyId)
        ON dbo.Rooms,  
    ADD BLOCK PREDICATE Security.fn_CheckMatchingCompany(CompanyId)
        ON dbo.Rooms AFTER INSERT,
	ADD BLOCK PREDICATE Security.fn_CheckMatchingCompany(CompanyId)
        ON dbo.Rooms AFTER UPDATE
    WITH (STATE = ON); 
GO

CREATE SECURITY POLICY Security.MessagesFilter
    ADD FILTER PREDICATE Security.fn_CheckMessages(RoomId)
        ON dbo.Messages,  
    ADD BLOCK PREDICATE Security.fn_CheckMessages(RoomId)
        ON dbo.Messages AFTER INSERT,
	ADD BLOCK PREDICATE Security.fn_CheckMessages(RoomId)
        ON dbo.Messages AFTER UPDATE
    WITH (STATE = ON); 
GO



-- Playground
	
	SELECT IS_SRVROLEMEMBER(N'sysadmin')
	SELECT IS_ROLEMEMBER ('db_owner', 'ChatApp')

	EXECUTE AS USER = 'ChatApp'; 
	REVERT
	
	EXEC sp_set_session_context @key=N'CompanyId', @value='Acme'
	
    SELECT * FROM rooms
    SELECT * FROM messages

    SELECT SESSION_CONTEXT(N'CompanyId')

    INSERT INTO rooms (NAME, CompanyId) VALUES ('not in', 'Acme')

    INSERT INTO [dbo].[Messages]
               ([TimeStamp]
               ,[Username]
               ,[Content]
               ,[RoomId])
         VALUES
               ('2020-1-1'
               ,'test'
               ,'some message'
		       ,1)