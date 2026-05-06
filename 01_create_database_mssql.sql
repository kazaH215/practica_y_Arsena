-- =====================================================
-- БД: Система управления производством
-- Для Microsoft SQL Server (ФИНАЛЬНАЯ РАБОЧАЯ)
-- =====================================================

USE master;
GO

-- Принудительно закрываем все соединения и удаляем базу
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'agrochem_db')
BEGIN
    ALTER DATABASE agrochem_db SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE agrochem_db;
END
GO

-- Создаём базу данных заново
CREATE DATABASE agrochem_db;
GO

USE agrochem_db;
GO

-- =====================================================
-- Удаляем таблицы, если существуют
-- =====================================================
IF OBJECT_ID('batch_raw_usage', 'U') IS NOT NULL DROP TABLE batch_raw_usage;
IF OBJECT_ID('raw_material_batches', 'U') IS NOT NULL DROP TABLE raw_material_batches;
IF OBJECT_ID('batch_step_execution', 'U') IS NOT NULL DROP TABLE batch_step_execution;
IF OBJECT_ID('deviations', 'U') IS NOT NULL DROP TABLE deviations;
IF OBJECT_ID('lab_test_parameters', 'U') IS NOT NULL DROP TABLE lab_test_parameters;
IF OBJECT_ID('lab_tests', 'U') IS NOT NULL DROP TABLE lab_tests;
IF OBJECT_ID('production_batches', 'U') IS NOT NULL DROP TABLE production_batches;
IF OBJECT_ID('production_orders', 'U') IS NOT NULL DROP TABLE production_orders;
IF OBJECT_ID('tech_steps', 'U') IS NOT NULL DROP TABLE tech_steps;
IF OBJECT_ID('tech_cards', 'U') IS NOT NULL DROP TABLE tech_cards;
IF OBJECT_ID('recipe_components', 'U') IS NOT NULL DROP TABLE recipe_components;
IF OBJECT_ID('recipes', 'U') IS NOT NULL DROP TABLE recipes;
IF OBJECT_ID('audit_log', 'U') IS NOT NULL DROP TABLE audit_log;
IF OBJECT_ID('equipment', 'U') IS NOT NULL DROP TABLE equipment;
IF OBJECT_ID('raw_materials', 'U') IS NOT NULL DROP TABLE raw_materials;
IF OBJECT_ID('products', 'U') IS NOT NULL DROP TABLE products;
IF OBJECT_ID('users', 'U') IS NOT NULL DROP TABLE users;
GO

-- =====================================================
-- 1. Нормативно-справочная информация
-- =====================================================

CREATE TABLE users (
    id INT IDENTITY(1,1) PRIMARY KEY,
    username VARCHAR(100) NOT NULL UNIQUE,
    full_name VARCHAR(200) NOT NULL,
    role VARCHAR(50) NOT NULL CHECK (role IN ('technologist', 'operator', 'laboratory', 'admin', 'engineer', 'shift_supervisor', 'manager', 'analyst', 'observer')),
    email VARCHAR(100) UNIQUE,
    phone VARCHAR(20),
    department VARCHAR(100),
    is_active BIT DEFAULT 1,
    last_login DATETIME,
    created_at DATETIME DEFAULT GETDATE()
);
GO

CREATE TABLE products (
    id INT IDENTITY(1,1) PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(200) NOT NULL,
    type VARCHAR(50),
    form VARCHAR(50),
    status VARCHAR(20) DEFAULT 'active'
);
GO

CREATE TABLE raw_materials (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    category VARCHAR(50),
    unit VARCHAR(20) DEFAULT 'кг'
);
GO

CREATE TABLE equipment (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    line_id VARCHAR(50),
    status VARCHAR(20) DEFAULT 'active'
);
GO

-- =====================================================
-- 2. Рецептуры и технологические карты
-- =====================================================

CREATE TABLE recipes (
    id INT IDENTITY(1,1) PRIMARY KEY,
    product_id INT REFERENCES products(id) ON DELETE NO ACTION,
    version INT NOT NULL,
    status VARCHAR(20) DEFAULT 'draft' CHECK (status IN ('draft', 'approved', 'archived')),
    total_percent DECIMAL(5,2) DEFAULT 0,
    created_by INT REFERENCES users(id),
    created_at DATETIME DEFAULT GETDATE(),
    CONSTRAINT UQ_recipes_product_version UNIQUE (product_id, version)
);
GO

CREATE TABLE recipe_components (
    id INT IDENTITY(1,1) PRIMARY KEY,
    recipe_id INT REFERENCES recipes(id) ON DELETE CASCADE,
    raw_material_id INT REFERENCES raw_materials(id),
    percentage DECIMAL(5,2) NOT NULL,
    tolerance DECIMAL(5,2) DEFAULT 0,
    order_num INT NOT NULL,
    CONSTRAINT CHK_percentage_positive CHECK (percentage >= 0 AND percentage <= 100)
);
GO

CREATE TABLE tech_cards (
    id INT IDENTITY(1,1) PRIMARY KEY,
    product_id INT REFERENCES products(id) ON DELETE NO ACTION,
    version INT NOT NULL,
    status VARCHAR(20) DEFAULT 'draft' CHECK (status IN ('draft', 'approved', 'archived')),
    created_by INT REFERENCES users(id),
    created_at DATETIME DEFAULT GETDATE(),
    CONSTRAINT UQ_techcards_product_version UNIQUE (product_id, version)
);
GO

CREATE TABLE tech_steps (
    id INT IDENTITY(1,1) PRIMARY KEY,
    tech_card_id INT REFERENCES tech_cards(id) ON DELETE CASCADE,
    step_type VARCHAR(50) NOT NULL,
    order_num INT NOT NULL,
    is_mandatory BIT DEFAULT 1,
    instructions NVARCHAR(MAX),
    planned_params NVARCHAR(MAX)
);
GO

-- =====================================================
-- 3. Производство
-- =====================================================

CREATE TABLE production_orders (
    id INT IDENTITY(1,1) PRIMARY KEY,
    product_id INT REFERENCES products(id),
    recipe_id INT REFERENCES recipes(id),
    tech_card_id INT REFERENCES tech_cards(id),
    planned_qty DECIMAL(10,2) NOT NULL,
    status VARCHAR(20) DEFAULT 'planned',
    created_at DATETIME DEFAULT GETDATE()
);
GO

CREATE TABLE production_batches (
    id INT IDENTITY(1,1) PRIMARY KEY,
    batch_number VARCHAR(50) NOT NULL UNIQUE,
    order_id INT REFERENCES production_orders(id),
    start_time DATETIME,
    end_time DATETIME,
    status VARCHAR(20) DEFAULT 'planned',
    actual_quantity_kg DECIMAL(10,2) DEFAULT 0,
    CONSTRAINT CHK_actual_qty_positive CHECK (actual_quantity_kg >= 0)
);
GO

CREATE TABLE batch_step_execution (
    id INT IDENTITY(1,1) PRIMARY KEY,
    batch_id INT REFERENCES production_batches(id) ON DELETE CASCADE,
    step_id INT REFERENCES tech_steps(id),
    actual_params NVARCHAR(MAX),
    start_time DATETIME,
    end_time DATETIME,
    status VARCHAR(20) DEFAULT 'pending'
);
GO

CREATE TABLE raw_material_batches (
    id INT IDENTITY(1,1) PRIMARY KEY,
    material_id INT REFERENCES raw_materials(id),
    batch_number VARCHAR(50),
    supplier VARCHAR(100),
    arrival_date DATE,
    quantity DECIMAL(10,2),
    lab_status VARCHAR(20) DEFAULT 'pending'
);
GO

CREATE TABLE batch_raw_usage (
    id INT IDENTITY(1,1) PRIMARY KEY,
    batch_id INT REFERENCES production_batches(id) ON DELETE CASCADE,
    rm_batch_id INT REFERENCES raw_material_batches(id),
    quantity_kg DECIMAL(10,2) NOT NULL
);
GO

-- =====================================================
-- 4. Лаборатория
-- =====================================================

CREATE TABLE lab_tests (
    id INT IDENTITY(1,1) PRIMARY KEY,
    object_type VARCHAR(50) NOT NULL CHECK (object_type IN ('raw_material', 'production_batch')),
    object_id INT NOT NULL,
    test_type VARCHAR(50),
    status VARCHAR(20) DEFAULT 'in_progress',
    result VARCHAR(50),
    comment NVARCHAR(MAX),
    created_by INT REFERENCES users(id),
    created_at DATETIME DEFAULT GETDATE()
);
GO

CREATE TABLE lab_test_parameters (
    id INT IDENTITY(1,1) PRIMARY KEY,
    test_id INT REFERENCES lab_tests(id) ON DELETE CASCADE,
    parameter_name VARCHAR(100) NOT NULL,
    norm_min DECIMAL(10,2),
    norm_max DECIMAL(10,2),
    actual_value DECIMAL(10,2),
    is_passed BIT
);
GO

-- =====================================================
-- 5. События и аудит
-- =====================================================

CREATE TABLE deviations (
    id INT IDENTITY(1,1) PRIMARY KEY,
    batch_id INT REFERENCES production_batches(id) ON DELETE CASCADE,
    step_id INT REFERENCES tech_steps(id),
    parameter VARCHAR(100) NOT NULL,
    planned_value VARCHAR(100),
    actual_value VARCHAR(100),
    severity VARCHAR(20) CHECK (severity IN ('info', 'warning', 'critical')),
    comment NVARCHAR(MAX),
    created_at DATETIME DEFAULT GETDATE()
);
GO

CREATE TABLE audit_log (
    id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT REFERENCES users(id),
    action VARCHAR(100) NOT NULL,
    entity_type VARCHAR(50),
    entity_id INT,
    old_state NVARCHAR(MAX),
    new_state NVARCHAR(MAX),
    timestamp DATETIME DEFAULT GETDATE()
);
GO

-- =====================================================
-- 6. Индексы
-- =====================================================

CREATE INDEX idx_recipes_product_id ON recipes(product_id);
CREATE INDEX idx_recipes_status ON recipes(status);
CREATE INDEX idx_tech_cards_product_id ON tech_cards(product_id);
CREATE INDEX idx_tech_cards_status ON tech_cards(status);
CREATE INDEX idx_production_batches_order_id ON production_batches(order_id);
CREATE INDEX idx_production_batches_status ON production_batches(status);
CREATE INDEX idx_batch_step_execution_batch_id ON batch_step_execution(batch_id);
CREATE INDEX idx_lab_tests_object ON lab_tests(object_type, object_id);
CREATE INDEX idx_deviations_batch_id ON deviations(batch_id);
CREATE INDEX idx_audit_log_user_id ON audit_log(user_id);
CREATE INDEX idx_audit_log_timestamp ON audit_log(timestamp);
GO

-- =====================================================
-- 7. Ограничения (одна активная рецептура и ТК)
-- =====================================================

CREATE UNIQUE NONCLUSTERED INDEX idx_active_recipe_per_product
ON recipes (product_id)
WHERE status = 'approved';
GO

CREATE UNIQUE NONCLUSTERED INDEX idx_active_techcard_per_product
ON tech_cards (product_id)
WHERE status = 'approved';
GO

-- =====================================================
-- 8. Тестовые данные (БЕЗ ошибок дат)
-- =====================================================

INSERT INTO users (username, full_name, role, email, department, is_active) VALUES
('tech.ivanov', 'Иванов Иван Петрович', 'technologist', 'ivan.ivanov@agrocontrol.ru', 'Технологический отдел', 1),
('tech.petrova', 'Петрова Мария Сергеевна', 'technologist', 'maria.petrova@agrocontrol.ru', 'Технологический отдел', 1),
('operator.zavodov', 'Заводов Сергей Николаевич', 'operator', 'sergey.zavodov@agrocontrol.ru', 'Цех №1', 1),
('lab.vasilieva', 'Васильева Елена Андреевна', 'laboratory', 'elena.vasilieva@agrocontrol.ru', 'Лаборатория контроля качества', 1),
('admin.sidorov', 'Сидоров Константин Петрович', 'admin', 'admin@agrocontrol.ru', 'IT-отдел', 1);
GO

INSERT INTO products (code, name, type, form, status) VALUES 
('HERB-001', 'Гербицид "Агро-К"', 'гербицид', 'концентрат', 'active'),
('INS-002', 'Инсектицид "Борей"', 'инсектицид', 'эмульсия', 'active');
GO

INSERT INTO raw_materials (name, category, unit) VALUES
('Действующее вещество А', 'активное', 'кг'),
('Растворитель Б', 'растворитель', 'л'),
('Эмульгатор В', 'эмульгатор', 'кг');
GO

-- Создаём производственные заказы (используем GETDATE() вместо фиксированных дат)
INSERT INTO production_orders (product_id, recipe_id, tech_card_id, planned_qty, status) VALUES
(1, NULL, NULL, 1000, 'completed'),
(1, NULL, NULL, 1000, 'completed'),
(1, NULL, NULL, 500, 'running'),
(2, NULL, NULL, 400, 'running'),
(1, NULL, NULL, 300, 'completed');
GO

-- Вставляем партии (используем GETDATE() для текущих дат)
INSERT INTO production_batches (batch_number, order_id, start_time, end_time, status, actual_quantity_kg) VALUES
('B-2401-01', 1, DATEADD(day, -30, GETDATE()), DATEADD(day, -29, GETDATE()), 'completed', 998),
('B-2401-02', 1, DATEADD(day, -28, GETDATE()), DATEADD(day, -27, GETDATE()), 'completed', 1002),
('B-2402-01', 2, DATEADD(day, -25, GETDATE()), NULL, 'running', 250),
('B-2404-01', 4, DATEADD(day, -20, GETDATE()), NULL, 'running', 400),
('B-2405-01', 5, DATEADD(day, -30, GETDATE()), DATEADD(day, -29, GETDATE()), 'completed', 298);
GO

-- Проверка результатов
PRINT '========================================';
PRINT '✅ БД успешно создана!';
PRINT '========================================';

SELECT 'users' AS table_name, COUNT(*) AS count FROM users
UNION ALL
SELECT 'products', COUNT(*) FROM products
UNION ALL
SELECT 'raw_materials', COUNT(*) FROM raw_materials
UNION ALL
SELECT 'production_orders', COUNT(*) FROM production_orders
UNION ALL
SELECT 'production_batches', COUNT(*) FROM production_batches;
GO