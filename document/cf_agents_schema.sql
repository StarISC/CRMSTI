IF OBJECT_ID('dbo.cf_agents', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.cf_agents (
        id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        code NVARCHAR(50) NULL,
        name NVARCHAR(255) NOT NULL,
        phone NVARCHAR(50) NULL,
        email NVARCHAR(255) NULL,
        tax_code NVARCHAR(50) NULL,
        tax_address NVARCHAR(500) NULL,
        status NVARCHAR(50) NULL,
        note NVARCHAR(1000) NULL,
        agent_type NVARCHAR(20) NOT NULL CONSTRAINT DF_cf_agents_type DEFAULT ('COMPANY'),
        parent_agent_id INT NULL,
        representative_name NVARCHAR(255) NULL,
        representative_phone NVARCHAR(50) NULL,
        commission_rate FLOAT NULL,
        contract_no NVARCHAR(100) NULL,
        contract_date DATETIME NULL,
        contract_expiry DATETIME NULL,
        license_no NVARCHAR(100) NULL,
        license_date DATETIME NULL,
        license_expiry DATETIME NULL,
        created_at DATETIME NOT NULL CONSTRAINT DF_cf_agents_created_at DEFAULT (GETDATE())
    );

    CREATE INDEX IX_cf_agents_phone ON dbo.cf_agents(phone);
END;
GO

IF OBJECT_ID('dbo.cf_agents', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.cf_agents', 'tax_address') IS NULL
        ALTER TABLE dbo.cf_agents ADD tax_address NVARCHAR(500) NULL;
    IF COL_LENGTH('dbo.cf_agents', 'contract_no') IS NULL
        ALTER TABLE dbo.cf_agents ADD contract_no NVARCHAR(100) NULL;
    IF COL_LENGTH('dbo.cf_agents', 'contract_date') IS NULL
        ALTER TABLE dbo.cf_agents ADD contract_date DATETIME NULL;
    IF COL_LENGTH('dbo.cf_agents', 'contract_expiry') IS NULL
        ALTER TABLE dbo.cf_agents ADD contract_expiry DATETIME NULL;
    IF COL_LENGTH('dbo.cf_agents', 'license_no') IS NULL
        ALTER TABLE dbo.cf_agents ADD license_no NVARCHAR(100) NULL;
    IF COL_LENGTH('dbo.cf_agents', 'license_date') IS NULL
        ALTER TABLE dbo.cf_agents ADD license_date DATETIME NULL;
    IF COL_LENGTH('dbo.cf_agents', 'license_expiry') IS NULL
        ALTER TABLE dbo.cf_agents ADD license_expiry DATETIME NULL;
END;
GO

IF OBJECT_ID('dbo.cf_agent_users', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.cf_agent_users (
        id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        agent_id INT NOT NULL,
        username NVARCHAR(100) NOT NULL,
        password NVARCHAR(255) NOT NULL,
        full_name NVARCHAR(255) NULL,
        phone NVARCHAR(50) NULL,
        email NVARCHAR(255) NULL,
        is_active BIT NOT NULL CONSTRAINT DF_cf_agent_users_is_active DEFAULT (1),
        created_at DATETIME NOT NULL CONSTRAINT DF_cf_agent_users_created_at DEFAULT (GETDATE())
    );

    ALTER TABLE dbo.cf_agent_users
        ADD CONSTRAINT FK_cf_agent_users_agent
        FOREIGN KEY (agent_id) REFERENCES dbo.cf_agents(id);

    CREATE UNIQUE INDEX UX_cf_agent_users_username ON dbo.cf_agent_users(username);
    CREATE INDEX IX_cf_agent_users_agent ON dbo.cf_agent_users(agent_id);
END;
GO

IF OBJECT_ID('dbo.cf_agents', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_cf_agents_parent')
    BEGIN
        ALTER TABLE dbo.cf_agents
            ADD CONSTRAINT FK_cf_agents_parent
            FOREIGN KEY (parent_agent_id) REFERENCES dbo.cf_agents(id);
    END
END;
GO

IF OBJECT_ID('dbo.cf_provinces', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.cf_provinces (
        id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        name NVARCHAR(255) NOT NULL,
        slug NVARCHAR(255) NULL,
        is_active BIT NOT NULL CONSTRAINT DF_cf_provinces_is_active DEFAULT (1),
        created_at DATETIME NOT NULL CONSTRAINT DF_cf_provinces_created_at DEFAULT (GETDATE())
    );
    CREATE UNIQUE INDEX UX_cf_provinces_name ON dbo.cf_provinces(name);
END;
GO

IF OBJECT_ID('dbo.cf_wards', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.cf_wards (
        id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        province_id INT NOT NULL,
        name NVARCHAR(255) NOT NULL,
        slug NVARCHAR(255) NULL,
        is_active BIT NOT NULL CONSTRAINT DF_cf_wards_is_active DEFAULT (1),
        created_at DATETIME NOT NULL CONSTRAINT DF_cf_wards_created_at DEFAULT (GETDATE())
    );

    ALTER TABLE dbo.cf_wards
        ADD CONSTRAINT FK_cf_wards_province
        FOREIGN KEY (province_id) REFERENCES dbo.cf_provinces(id);

    CREATE UNIQUE INDEX UX_cf_wards_province_name ON dbo.cf_wards(province_id, name);
END;
GO

IF OBJECT_ID('dbo.cf_agent_addresses', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.cf_agent_addresses (
        id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        agent_id INT NOT NULL,
        house_no NVARCHAR(50) NULL,
        street NVARCHAR(255) NULL,
        ward_id INT NOT NULL,
        province_id INT NOT NULL,
        full_address NVARCHAR(600) NULL,
        note NVARCHAR(500) NULL,
        created_at DATETIME NOT NULL CONSTRAINT DF_cf_agent_addresses_created_at DEFAULT (GETDATE())
    );

    ALTER TABLE dbo.cf_agent_addresses
        ADD CONSTRAINT FK_cf_agent_addresses_agent
        FOREIGN KEY (agent_id) REFERENCES dbo.cf_agents(id);

    ALTER TABLE dbo.cf_agent_addresses
        ADD CONSTRAINT FK_cf_agent_addresses_ward
        FOREIGN KEY (ward_id) REFERENCES dbo.cf_wards(id);

    ALTER TABLE dbo.cf_agent_addresses
        ADD CONSTRAINT FK_cf_agent_addresses_province
        FOREIGN KEY (province_id) REFERENCES dbo.cf_provinces(id);

    CREATE INDEX IX_cf_agent_addresses_agent ON dbo.cf_agent_addresses(agent_id);
    CREATE INDEX IX_cf_agent_addresses_province ON dbo.cf_agent_addresses(province_id);
END;
GO

IF OBJECT_ID('dbo.cf_agent_documents', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.cf_agent_documents (
        id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        agent_id INT NOT NULL,
        doc_type NVARCHAR(50) NOT NULL,
        doc_no NVARCHAR(100) NULL,
        doc_date DATETIME NULL,
        file_name NVARCHAR(255) NULL,
        file_path NVARCHAR(500) NULL,
        note NVARCHAR(500) NULL,
        created_at DATETIME NOT NULL CONSTRAINT DF_cf_agent_documents_created_at DEFAULT (GETDATE())
    );

    ALTER TABLE dbo.cf_agent_documents
        ADD CONSTRAINT FK_cf_agent_documents_agent
        FOREIGN KEY (agent_id) REFERENCES dbo.cf_agents(id);

    CREATE INDEX IX_cf_agent_documents_agent ON dbo.cf_agent_documents(agent_id);
    CREATE INDEX IX_cf_agent_documents_type ON dbo.cf_agent_documents(doc_type);
END;
GO

IF OBJECT_ID('dbo.TR_cf_agents_code', 'TR') IS NOT NULL
    DROP TRIGGER dbo.TR_cf_agents_code;
GO

CREATE TRIGGER dbo.TR_cf_agents_code
ON dbo.cf_agents
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE a
    SET a.code = 'AG' + RIGHT('000000' + CAST(a.id AS VARCHAR(6)), 6)
    FROM dbo.cf_agents a
    INNER JOIN inserted i ON a.id = i.id
    WHERE (i.code IS NULL OR LTRIM(RTRIM(i.code)) = '');
END;
GO
