-- =====================================================
-- Триггеры для системы управления производством
-- Microsoft SQL Server (ИСПРАВЛЕННАЯ)
-- =====================================================

USE agrochem_db;
GO

-- =====================================================
-- Триггер 1: Проверка суммы компонентов рецептуры (должна быть = 100%)
-- =====================================================
CREATE OR ALTER TRIGGER trg_check_recipe_before_approve
ON recipes
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    IF UPDATE(status)
    BEGIN
        DECLARE @recipe_id INT;
        DECLARE @new_status VARCHAR(20);
        DECLARE @total_sum DECIMAL(5,2);
        DECLARE @total_sum_str VARCHAR(20);
        
        -- Получаем изменённые записи
        DECLARE cur CURSOR FOR
            SELECT i.id, i.status
            FROM inserted i
            WHERE i.status = 'approved';
        
        OPEN cur;
        FETCH NEXT FROM cur INTO @recipe_id, @new_status;
        
        WHILE @@FETCH_STATUS = 0
        BEGIN
            -- Считаем сумму компонентов
            SELECT @total_sum = COALESCE(SUM(percentage), 0)
            FROM recipe_components
            WHERE recipe_id = @recipe_id;
            
            -- Проверяем сумму
            IF @total_sum <> 100
            BEGIN
                -- Откатываем транзакцию
                ROLLBACK TRANSACTION;
                SET @total_sum_str = CAST(@total_sum AS VARCHAR(20));
                RAISERROR('Невозможно утвердить рецептуру %d: сумма компонентов = %s%%, а должна быть 100%%', 16, 1, @recipe_id, @total_sum_str);
                RETURN;
            END
            ELSE
            BEGIN
                -- Обновляем total_percent в рецептуре
                UPDATE recipes 
                SET total_percent = @total_sum
                WHERE id = @recipe_id;
            END
            
            FETCH NEXT FROM cur INTO @recipe_id, @new_status;
        END
        
        CLOSE cur;
        DEALLOCATE cur;
    END
END;
GO

-- =====================================================
-- Триггер 2: Аудит изменений статуса рецептуры
-- =====================================================
CREATE OR ALTER TRIGGER trg_recipe_status_audit
ON recipes
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    IF UPDATE(status)
    BEGIN
        INSERT INTO audit_log (user_id, action, entity_type, entity_id, old_state, new_state, timestamp)
        SELECT 
            1 AS user_id,
            'change_recipe_status' AS action,
            'recipe' AS entity_type,
            i.id AS entity_id,
            ('{"status":"' + d.status + '", "version":"' + CAST(d.version AS VARCHAR) + '"}') AS old_state,
            ('{"status":"' + i.status + '", "version":"' + CAST(i.version AS VARCHAR) + '"}') AS new_state,
            GETDATE() AS timestamp
        FROM inserted i
        INNER JOIN deleted d ON i.id = d.id
        WHERE i.status != d.status;
    END
END;
GO

-- =====================================================
-- Триггер 3: Автоматическое обновление total_percent при изменении компонентов
-- =====================================================
CREATE OR ALTER TRIGGER trg_update_recipe_total
ON recipe_components
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @recipe_id INT;
    
    -- Собираем все затронутые recipe_id
    DECLARE cur CURSOR FOR
        SELECT DISTINCT recipe_id FROM inserted
        UNION
        SELECT DISTINCT recipe_id FROM deleted;
    
    OPEN cur;
    FETCH NEXT FROM cur INTO @recipe_id;
    
    WHILE @@FETCH_STATUS = 0 AND @recipe_id IS NOT NULL
    BEGIN
        -- Пересчитываем сумму
        UPDATE recipes 
        SET total_percent = (
            SELECT COALESCE(SUM(percentage), 0)
            FROM recipe_components
            WHERE recipe_id = @recipe_id
        )
        WHERE id = @recipe_id;
        
        FETCH NEXT FROM cur INTO @recipe_id;
    END
    
    CLOSE cur;
    DEALLOCATE cur;
END;
GO

-- =====================================================
-- Триггер 4: Защита от изменения завершённой партии
-- =====================================================
CREATE OR ALTER TRIGGER trg_prevent_closed_batch_change
ON production_batches
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    IF UPDATE(status) OR UPDATE(start_time) OR UPDATE(end_time)
    BEGIN
        IF EXISTS (
            SELECT 1
            FROM inserted i
            INNER JOIN deleted d ON i.id = d.id
            WHERE d.status = 'completed' AND i.status != d.status
        )
        BEGIN
            ROLLBACK TRANSACTION;
            RAISERROR('Невозможно изменить завершённую партию', 16, 1);
            RETURN;
        END
    END
END;
GO

-- =====================================================
-- Триггер 5: Логирование создания партии
-- =====================================================
CREATE OR ALTER TRIGGER trg_batch_created_audit
ON production_batches
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO audit_log (user_id, action, entity_type, entity_id, new_state, timestamp)
    SELECT 
        1 AS user_id,
        'create_batch' AS action,
        'production_batch' AS entity_type,
        i.id AS entity_id,
        ('{"batch_number":"' + i.batch_number + '", "status":"' + i.status + '"}') AS new_state,
        GETDATE() AS timestamp
    FROM inserted i;
END;
GO

-- =====================================================
-- Проверка создания триггеров
-- =====================================================
SELECT 
    name AS trigger_name,
    OBJECT_NAME(parent_id) AS table_name,
    is_disabled
FROM sys.triggers
WHERE parent_class = 1
ORDER BY name;

PRINT '✅ Все триггеры успешно созданы!';
GO