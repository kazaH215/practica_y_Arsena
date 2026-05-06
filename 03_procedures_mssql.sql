-- =====================================================
-- Хранимые процедуры для системы управления производством
-- Microsoft SQL Server (ИСПРАВЛЕННАЯ)
-- =====================================================

USE agrochem_db;
GO

-- =====================================================
-- Процедура 1: Запуск производственной партии
-- =====================================================
CREATE OR ALTER PROCEDURE sp_start_production_batch
    @p_batch_id INT,
    @p_user_id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @v_batch_number VARCHAR(50);
    DECLARE @v_user_role VARCHAR(50);
    DECLARE @v_user_name VARCHAR(200);
    DECLARE @v_batch_status VARCHAR(20);
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Получаем данные партии
        SELECT @v_batch_number = batch_number, @v_batch_status = status
        FROM production_batches
        WHERE id = @p_batch_id;
        
        IF @v_batch_number IS NULL
        BEGIN
            RAISERROR('Партия с ID %d не найдена', 16, 1, @p_batch_id);
            RETURN;
        END
        
        -- Проверка прав пользователя
        SELECT @v_user_role = role, @v_user_name = full_name
        FROM users
        WHERE id = @p_user_id;
        
        IF @v_user_role IS NULL
        BEGIN
            RAISERROR('Пользователь с ID %d не найден', 16, 1, @p_user_id);
            RETURN;
        END
        
        IF @v_user_role NOT IN ('technologist', 'shift_supervisor', 'admin')
        BEGIN
            RAISERROR('Доступ запрещён: пользователь "%s" (роль %s) не имеет прав', 16, 1, @v_user_name, @v_user_role);
            RETURN;
        END
        
        -- Проверка статуса партии
        IF @v_batch_status <> 'planned'
        BEGIN
            RAISERROR('Невозможно запустить партию "%s": текущий статус "%s"', 16, 1, @v_batch_number, @v_batch_status);
            RETURN;
        END
        
        -- Запуск партии
        UPDATE production_batches
        SET status = 'running', 
            start_time = GETDATE()
        WHERE id = @p_batch_id;
        
        -- Аудит
        INSERT INTO audit_log (user_id, action, entity_type, entity_id, new_state, timestamp)
        VALUES (
            @p_user_id, 
            'start_batch', 
            'production_batch', 
            @p_batch_id, 
            '{"status":"running"}',
            GETDATE()
        );
        
        COMMIT TRANSACTION;
        
        PRINT '✅ Партия "' + @v_batch_number + '" успешно запущена пользователем "' + @v_user_name + '"';
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        DECLARE @err_msg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR('Ошибка при запуске партии: %s', 16, 1, @err_msg);
    END CATCH
END;
GO

-- =====================================================
-- Процедура 2: Завершение производственной партии (с проверкой лаборатории)
-- =====================================================
CREATE OR ALTER PROCEDURE sp_complete_batch_with_lab_check
    @p_batch_id INT,
    @p_user_id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @v_lab_result VARCHAR(50);
    DECLARE @v_user_role VARCHAR(50);
    DECLARE @v_batch_status VARCHAR(50);
    DECLARE @v_batch_number VARCHAR(50);
    DECLARE @v_user_name VARCHAR(200);
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- 1. Получаем данные партии
        SELECT @v_batch_number = batch_number, @v_batch_status = status
        FROM production_batches
        WHERE id = @p_batch_id;
        
        IF @v_batch_number IS NULL
        BEGIN
            RAISERROR('Партия с ID %d не найдена', 16, 1, @p_batch_id);
            RETURN;
        END
        
        -- 2. Проверка прав пользователя
        SELECT @v_user_role = role, @v_user_name = full_name
        FROM users
        WHERE id = @p_user_id;
        
        IF @v_user_role IS NULL
        BEGIN
            RAISERROR('Пользователь с ID %d не найден', 16, 1, @p_user_id);
            RETURN;
        END
        
        IF @v_user_role NOT IN ('technologist', 'shift_supervisor', 'admin')
        BEGIN
            RAISERROR('Доступ запрещён: пользователь "%s" (роль %s) не имеет прав', 16, 1, @v_user_name, @v_user_role);
            RETURN;
        END
        
        -- 3. Проверка статуса партии
        IF @v_batch_status = 'completed'
        BEGIN
            RAISERROR('Партия "%s" уже завершена', 16, 1, @v_batch_number);
            RETURN;
        END
        
        IF @v_batch_status = 'planned'
        BEGIN
            RAISERROR('Партия "%s" ещё не запущена', 16, 1, @v_batch_number);
            RETURN;
        END
        
        -- 4. Проверка лабораторного решения
        SELECT TOP 1 @v_lab_result = result
        FROM lab_tests
        WHERE object_type = 'production_batch' 
          AND object_id = @p_batch_id
          AND status = 'completed'
        ORDER BY created_at DESC;
        
        IF @v_lab_result IS NULL
        BEGIN
            RAISERROR('Невозможно завершить партию "%s": лабораторные испытания не проведены', 16, 1, @v_batch_number);
            RETURN;
        END
        
        IF @v_lab_result != 'approved'
        BEGIN
            RAISERROR('Невозможно завершить партию "%s": лабораторное решение - "%s"', 16, 1, @v_batch_number, @v_lab_result);
            RETURN;
        END
        
        -- 5. Завершение партии
        UPDATE production_batches
        SET status = 'completed', 
            end_time = GETDATE()
        WHERE id = @p_batch_id;
        
        -- 6. Запись в аудит
        INSERT INTO audit_log (user_id, action, entity_type, entity_id, new_state, timestamp)
        VALUES (
            @p_user_id, 
            'complete_batch', 
            'production_batch', 
            @p_batch_id, 
            '{"status":"completed"}',
            GETDATE()
        );
        
        COMMIT TRANSACTION;
        
        PRINT '✅ Партия "' + @v_batch_number + '" успешно завершена пользователем "' + @v_user_name + '"';
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        DECLARE @err_msg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR('Ошибка при завершении партии: %s', 16, 1, @err_msg);
    END CATCH
END;
GO

-- =====================================================
-- Процедура 3: Просмотр активных партий
-- =====================================================
CREATE OR ALTER PROCEDURE sp_get_active_batches
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        b.id,
        b.batch_number,
        ISNULL(p.name, 'Нет продукта') AS product_name,
        b.status,
        b.start_time,
        b.actual_quantity_kg,
        CASE 
            WHEN EXISTS (SELECT 1 FROM deviations WHERE batch_id = b.id AND severity = 'critical') THEN 'Критическое'
            WHEN EXISTS (SELECT 1 FROM deviations WHERE batch_id = b.id AND severity = 'warning') THEN 'Предупреждение'
            ELSE 'Норма'
        END AS deviation_status
    FROM production_batches b
    LEFT JOIN production_orders o ON b.order_id = o.id
    LEFT JOIN products p ON o.product_id = p.id
    WHERE b.status IN ('running', 'planned')
    ORDER BY b.start_time DESC;
END;
GO

-- =====================================================
-- Процедура 4: Получение статистики для главной страницы технолога
-- =====================================================
CREATE OR ALTER PROCEDURE sp_get_technologist_dashboard
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Активные продукты
    SELECT COUNT(*) AS active_products FROM products WHERE status = 'active';
    
    -- Действующие рецептуры
    SELECT COUNT(*) AS approved_recipes FROM recipes WHERE status = 'approved';
    
    -- Действующие ТК
    SELECT COUNT(*) AS approved_tech_cards FROM tech_cards WHERE status = 'approved';
    
    -- Заказы в работе
    SELECT COUNT(*) AS active_orders FROM production_orders WHERE status IN ('planned', 'running');
    
    -- Партии в производстве
    SELECT COUNT(*) AS running_batches FROM production_batches WHERE status = 'running';
    
    -- Партии с отклонениями
    SELECT COUNT(DISTINCT batch_id) AS batches_with_deviations FROM deviations WHERE severity IN ('warning', 'critical');
    
    -- Партии, ожидающие лабораторию
    SELECT COUNT(*) AS pending_lab_batches 
    FROM production_batches b
    WHERE b.status = 'running' 
      AND NOT EXISTS (SELECT 1 FROM lab_tests WHERE object_type = 'production_batch' AND object_id = b.id);
END;
GO

-- =====================================================
-- Проверка создания процедур
-- =====================================================
SELECT 
    name AS procedure_name,
    type_desc
FROM sys.procedures
ORDER BY name;

PRINT '✅ Все хранимые процедуры успешно созданы!';
PRINT '';
PRINT '========================================';
PRINT 'Примеры вызова процедур:';
PRINT '========================================';
PRINT '-- Запустить партию: EXEC sp_start_production_batch 3, 1;';
PRINT '-- Завершить партию: EXEC sp_complete_batch_with_lab_check 3, 1;';
PRINT '-- Активные партии: EXEC sp_get_active_batches;';
PRINT '-- Статистика: EXEC sp_get_technologist_dashboard;';
GO