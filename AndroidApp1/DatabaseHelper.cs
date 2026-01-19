using Android.Content;
using Android.Database;
using Android.Database.Sqlite;
using AndroidApp1;
using System.Collections.Generic;
using System; 

public class DatabaseHelper : SQLiteOpenHelper
{
    private const string DATABASE_NAME = "BudgetDB.db";
    // Incrementing version to 2 to trigger OnUpgrade and add the Date column
    private const int DATABASE_VERSION = 2;

    private const string TABLE_EXPENSES = "Expenses";
    private const string COL_ID = "Id";
    private const string COL_DESC = "Description";
    private const string COL_AMOUNT = "Amount";
    private const string COL_CATEGORY = "Category";
    private const string COL_DATE = "Date"; 

    private const string TABLE_BUDGET = "Budget";
    private const string COL_BUDGET_AMOUNT = "Amount";

    public DatabaseHelper(Context context) : base(context, DATABASE_NAME, null, DATABASE_VERSION) { }

    public override void OnCreate(SQLiteDatabase db)
    {
        // Added COL_DATE to the table creation string
        db.ExecSQL($"CREATE TABLE {TABLE_EXPENSES} ({COL_ID} INTEGER PRIMARY KEY AUTOINCREMENT, {COL_DESC} TEXT, {COL_AMOUNT} REAL, {COL_CATEGORY} TEXT, {COL_DATE} TEXT)");
        db.ExecSQL($"CREATE TABLE {TABLE_BUDGET} ({COL_BUDGET_AMOUNT} REAL)");
    }

    public override void OnUpgrade(SQLiteDatabase db, int oldVersion, int newVersion)
    {
        if (oldVersion < 2)
        {
            // If the user already had version 1, add the Date column without deleting their data
            db.ExecSQL($"ALTER TABLE {TABLE_EXPENSES} ADD COLUMN {COL_DATE} TEXT");
        }
        else
        {
            // Fallback for other major changes
            db.ExecSQL($"DROP TABLE IF EXISTS {TABLE_EXPENSES}");
            db.ExecSQL($"DROP TABLE IF EXISTS {TABLE_BUDGET}");
            OnCreate(db);
        }
    }

    // ===== Budget =====
    public void SaveBudget(double amount)
    {
        SQLiteDatabase db = WritableDatabase;
        db.ExecSQL($"DELETE FROM {TABLE_BUDGET}");
        ContentValues values = new ContentValues();
        values.Put(COL_BUDGET_AMOUNT, amount);
        db.Insert(TABLE_BUDGET, null, values);
    }

    public double GetBudget()
    {
        double budget = 0;
        SQLiteDatabase db = ReadableDatabase;
        ICursor cursor = db.RawQuery($"SELECT {COL_BUDGET_AMOUNT} FROM {TABLE_BUDGET} LIMIT 1", null);
        if (cursor.MoveToFirst())
            budget = cursor.GetDouble(0);
        cursor.Close();
        return budget;
    }

    // ===== Expenses =====
    public void AddExpense(Expense exp)
    {
        SQLiteDatabase db = WritableDatabase;
        ContentValues values = new ContentValues();
        values.Put(COL_DESC, exp.Description);
        values.Put(COL_AMOUNT, exp.Amount);
        values.Put(COL_CATEGORY, exp.Category);
        // Automatically save the current date/time
        values.Put(COL_DATE, DateTime.Now.ToString("MMM dd, yyyy HH:mm"));

        db.Insert(TABLE_EXPENSES, null, values);
    }

    public List<Expense> GetAllExpenses()
    {
        List<Expense> list = new List<Expense>();
        SQLiteDatabase db = ReadableDatabase;
        // Order by ID DESC so the newest expenses appear at the top of the list
        ICursor cursor = db.RawQuery($"SELECT * FROM {TABLE_EXPENSES} ORDER BY {COL_ID} DESC", null);

        while (cursor.MoveToNext())
        {
            list.Add(new Expense
            {
                Id = cursor.GetInt(cursor.GetColumnIndex(COL_ID)),
                Description = cursor.GetString(cursor.GetColumnIndex(COL_DESC)),
                Amount = cursor.GetDouble(cursor.GetColumnIndex(COL_AMOUNT)),
                Category = cursor.GetString(cursor.GetColumnIndex(COL_CATEGORY)),
                Date = cursor.GetString(cursor.GetColumnIndex(COL_DATE)) // Read the date
            });
        }
        cursor.Close();
        return list;
    }

    public void UpdateExpense(Expense exp)
    {
        SQLiteDatabase db = WritableDatabase;
        ContentValues values = new ContentValues();
        values.Put(COL_DESC, exp.Description);
        values.Put(COL_AMOUNT, exp.Amount);
        values.Put(COL_CATEGORY, exp.Category);
        // Update keeps the original date unless you choose to refresh it
        db.Update(TABLE_EXPENSES, values, $"{COL_ID} = ?", new string[] { exp.Id.ToString() });
    }

    public void DeleteExpense(int id)
    {
        SQLiteDatabase db = WritableDatabase;
        db.Delete(TABLE_EXPENSES, $"{COL_ID} = ?", new string[] { id.ToString() });
    }
}