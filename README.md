# Hardware Store — Multi-vendor Computer Hardware Online Store

Graduation project: ASP.NET Core MVC (.NET 10) + Entity Framework Core + PostgreSQL 16 + Bootstrap 5 + Chart.js + cropper.js.

This package contains the **complete application**: data layer, storefront, shopping flow, merchant back office and administrator back office.

---

## 1. Environment preparation (install once on your own computer)

1. Install **.NET 10 SDK**: https://dotnet.microsoft.com/download/dotnet/10.0
   Verify in a terminal:
   ```bash
   dotnet --version
   ```
2. Install **PostgreSQL 16** and start the local service (default port 5432).
3. Install the EF Core command-line tool (once):
   ```bash
   dotnet tool install --global dotnet-ef
   ```

## 2. Create the database

Open PostgreSQL (pgAdmin or `psql`) and create an empty database:

```sql
CREATE DATABASE "HardwareStore";
```

## 3. Configure the connection string

Open `appsettings.json` and set your PostgreSQL username and password:

```json
"ConnectionStrings": {
  "Default": "Host=localhost;Port=5432;Database=HardwareStore;Username=postgres;Password=your_password;"
}
```

## 4. Restore packages and create the database schema

Run these commands inside the `HardwareStore` folder (the folder that contains `HardwareStore.csproj`):

```bash
dotnet restore
dotnet ef database update
```

- The initial migration (`Migrations/20261118215054_InitialCreate`) is already included in this package.
- `database update` applies it and creates all tables in PostgreSQL.
- If you change the entity classes later, create a new migration with `dotnet ef migrations add <Name>`.
- Demo data (users, merchants, categories, brands, products, SKUs, banners, notices) is inserted
  automatically on the first run by `Data/DbSeeder.cs`, so you do not need to type any data by hand.

## 5. Run the database-side script (roles, grants, optional triggers)

After the tables exist, run `Scripts/init.sql` against the `HardwareStore` database
(paste it into `psql` or pgAdmin query tool). It creates the three application roles and
grants table-level privileges.

> **Important — choose one stock mechanism.** `init.sql` also defines four reference triggers
> (`trg_stock_deduct`, `trg_stock_restore`, `trg_price_audit`, `trg_order_amount`). The application
> already deducts stock and restores it inside a transaction (`OrderController`), so **if you run
> the stock triggers as well, every sale would deduct stock twice**. For a normal run of the website,
> execute only the role/grant part of the script, or comment out the two stock triggers. Keep them
> uncommented only when you want to demonstrate database-side enforcement in the defence.

## 6. Run the application

```bash
dotnet run
```

Open the address shown in the terminal (default `http://localhost:5000`).

Uploaded product images, brand logos and banners are stored under `wwwroot/uploads/…`; the folder is
created automatically on first upload, nothing to prepare manually.

## 7. Demo accounts (seeded automatically)

| Role     | Username / e-mail        | Password      |
|----------|--------------------------|---------------|
| Admin    | admin                    | Admin@123     |
| Merchant | techstore@example.com    | Merchant@123  |
| Customer | customer                 | Customer@123  |

There are three approved demo merchants (`techstore@example.com`, `gamerzone@example.com`,
`officedepot@example.com`, all with `Merchant@123`) plus one merchant waiting for approval, so the
onboarding screen in the admin back office has something to show. The admin can reset a customer
password, which sets it to `Reset@123`.

## 8. Main routes

| Area | URL |
|------|-----|
| Storefront home | `/` |
| Product list / search | `/Product/List?keyword=ssd` |
| AJAX suggest endpoint | `/Product/Suggest?term=ss` |
| Product detail | `/Product/{id}` |
| Cart / checkout | `/Cart`, `/Order/Checkout` |
| My orders | `/Order/MyOrders` |
| Customer centre | `/User/Profile` |
| Merchant back office | `/Merchant` (login as merchant first) |
| Admin back office | `/Admin` |
| Statistics with charts | `/Admin/Stats` |
| Pending merchant applications | `/Admin/Merchants?status=pending` |

## Project layout

```
HardwareStore/
├── Controllers/        # Home, Account, Product, Cart, Order, Review, User, Merchant, Admin
├── Data/               # DbContext, password hasher, demo seeder
├── Models/
│   ├── Entities/       # One class per database table (20 tables)
│   ├── ViewModels.cs   # View models passed to the Razor pages
│   └── RoleConstants.cs
├── Services/           # Session helper, file storage for uploads
├── Scripts/init.sql    # Roles, grants, reference triggers
├── Migrations/         # InitialCreate migration
├── Views/              # Razor views and shared layout
├── wwwroot/            # Static images, css, js, uploads
└── appsettings.json    # Connection string and configuration
```

## Design rules implemented in the code

- All code, identifiers, comments and UI text are in English.
- Stock lives only on the SKU level (`ProductSku.stock`); the `Product` table has no stock field.
- Every merchant-side list query is filtered by `merchantID`, so a merchant can never see another
  shop's products, orders or reviews.
- A review can only be written for an order line whose order status is `completed`; the review row
  stores `OrderItemId`, and the order line is marked as reviewed.
- Product search is a text box plus a button, backed by a separate AJAX endpoint
  (`/Product/Suggest`) that returns JSON for the drop-down suggestions.
- Order status flow: `pending_payment → paid → delivered → received → completed`, with `canceled`
  allowed before delivery; canceling restores SKU stock and rolls back the sales counter.
- Shipping is free from 99, otherwise 10, calculated per merchant sub-order.
- Product images belong to merchants; brand logos and home-page banners belong to the administrator.
- Statistics are aggregated per day and rendered with Chart.js (sales line plus visits bar).
