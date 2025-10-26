INSERT INTO [dbo].[Roles] (RoleId, Name, Description, IsActive, CreatedDate)
VALUES 
(NEWID(), 'Student', 'A student or parent user profile', 1, GETUTCDATE()),
(NEWID(), 'Tutor', 'A tutor user profile', 1, GETUTCDATE()),
(NEWID(), 'Institute', 'An institute user profile', 1, GETUTCDATE());