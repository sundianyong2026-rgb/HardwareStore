-- ============================================================
-- Database-side security roles, grants and triggers.
-- Run this script AFTER the EF Core migration has created the tables.
-- Database: HardwareStore (PostgreSQL 16)
-- ============================================================

-- Application roles (row-level merchant isolation is enforced in the
-- application layer; these roles hold the table-level privileges).
CREATE USER customer_app WITH PASSWORD 'customer123';
CREATE USER merchant_app  WITH PASSWORD 'merchant123';
CREATE USER admin_app     WITH PASSWORD 'admin123';

GRANT CONNECT ON DATABASE "HardwareStore" TO customer_app;
GRANT CONNECT ON DATABASE "HardwareStore" TO merchant_app;
GRANT CONNECT ON DATABASE "HardwareStore" TO admin_app;

-- Customer: read catalogue, write own cart / address / favourite / history / review / message.
GRANT SELECT ON Product, ProductSku, ProductImage, ProductAttribute,
    ProductAttributeValue, Category, Brand TO customer_app;
GRANT INSERT, UPDATE, DELETE ON CartItem, Address, Favorite,
    ViewLog, Review, ContactMessage TO customer_app;
GRANT SELECT ON OrderInfo, OrderItem TO customer_app;
-- Customers need to pay, cancel and hide their own orders.
GRANT UPDATE (status, paidAt, hiddenByCustomer) ON OrderInfo TO customer_app;

-- Merchant: manage own catalogue and own orders / messages.
GRANT SELECT, INSERT, UPDATE, DELETE ON Product, ProductSku,
    ProductImage, ProductAttribute, ProductAttributeValue TO merchant_app;
GRANT SELECT, UPDATE ON OrderInfo, OrderItem TO merchant_app;
GRANT SELECT, UPDATE ON ContactMessage TO merchant_app;
GRANT UPDATE (shopName, contactName, phone, email, description, logoUrl)
    ON Merchant TO merchant_app;

-- Administrator: full privileges on the platform data.
GRANT ALL PRIVILEGES ON Admin, Merchant, Category, Brand, Product,
    ProductSku, ProductImage, ProductAttribute, ProductAttributeValue,
    Customer, Review, Banner, Notice, DailyVisit, ContactMessage TO admin_app;

-- ------------------------------------------------------------
-- Trigger: reject an order line when the SKU stock is insufficient.
-- ------------------------------------------------------------
CREATE OR REPLACE FUNCTION check_sku_stock()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM ProductSku
        WHERE skuID = NEW.skuID AND stock >= NEW.quantity
    ) THEN
        RAISE EXCEPTION 'Insufficient stock for SKU %', NEW.skuID;
        RETURN NULL;
    END IF;
    RETURN NEW;
END; $$;

CREATE TRIGGER trg_check_stock
    BEFORE INSERT ON OrderItem
    FOR EACH ROW EXECUTE PROCEDURE check_sku_stock();

-- ------------------------------------------------------------
-- Trigger: deduct SKU stock and increase product sales count.
-- ------------------------------------------------------------
CREATE OR REPLACE FUNCTION deduct_stock()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
BEGIN
    UPDATE ProductSku SET stock = stock - NEW.quantity
        WHERE skuID = NEW.skuID;
    UPDATE Product SET salesCount = salesCount + NEW.quantity
        WHERE productID = NEW.productID;
    RETURN NEW;
END; $$;

CREATE TRIGGER trg_deduct_stock
    AFTER INSERT ON OrderItem
    FOR EACH ROW EXECUTE PROCEDURE deduct_stock();

-- ------------------------------------------------------------
-- Trigger: restore stock and roll back sales count on cancellation.
-- ------------------------------------------------------------
CREATE OR REPLACE FUNCTION restore_stock()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
BEGIN
    IF NEW.status = 'canceled' AND OLD.status <> 'canceled' THEN
        UPDATE ProductSku s SET stock = stock + oi.quantity
            FROM OrderItem oi WHERE oi.skuID = s.skuID
            AND oi.orderID = NEW.orderID;
        UPDATE Product p SET salesCount = salesCount - oi.quantity
            FROM OrderItem oi WHERE oi.productID = p.productID
            AND oi.orderID = NEW.orderID;
    END IF;
    RETURN NEW;
END; $$;

CREATE TRIGGER trg_restore_stock
    AFTER UPDATE ON OrderInfo
    FOR EACH ROW EXECUTE PROCEDURE restore_stock();

-- ------------------------------------------------------------
-- Trigger: audit every SKU price change.
-- ------------------------------------------------------------
CREATE OR REPLACE FUNCTION log_price_change()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
BEGIN
    IF NEW.price <> OLD.price THEN
        INSERT INTO PriceChangeLog(productID, skuID, oldPrice, newPrice, changedAt)
        VALUES (OLD.productID, OLD.skuID, OLD.price, NEW.price, now());
    END IF;
    RETURN NEW;
END; $$;

CREATE TRIGGER trg_price_log
    AFTER UPDATE ON ProductSku
    FOR EACH ROW EXECUTE PROCEDURE log_price_change();
