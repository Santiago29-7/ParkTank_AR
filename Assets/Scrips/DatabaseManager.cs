using UnityEngine;
using System.Data;
using Mono.Data.Sqlite;
using System.IO;

public class DatabaseManager : MonoBehaviour
{
    private string dbPath;

    void Awake()
    {
        // Ruta persistente compatible con Android, iOS y PC
        string filepath = Path.Combine(Application.persistentDataPath, "player_data.db");
        dbPath = "URI=file:" + filepath;

        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using (IDbConnection conn = new SqliteConnection(dbPath))
        {
            conn.Open();
            using (IDbCommand cmd = conn.CreateCommand())
            {
                // Crear tabla de perfil si no existe
                cmd.CommandText = "CREATE TABLE IF NOT EXISTS PlayerProfile (id INTEGER PRIMARY KEY, nickname TEXT);";
                cmd.ExecuteNonQuery();
            }
            conn.Close();
        }
    }

    public void SaveNickname(string nickname)
    {
        using (IDbConnection conn = new SqliteConnection(dbPath))
        {
            conn.Open();
            using (IDbCommand cmd = conn.CreateCommand())
            {
                // Limpiar registros antiguos e insertar el nuevo nickname
                cmd.CommandText = "DELETE FROM PlayerProfile;";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "INSERT INTO PlayerProfile (id, nickname) VALUES (1, @nick);";

                IDbDataParameter param = cmd.CreateParameter();
                param.ParameterName = "@nick";
                param.Value = nickname;
                cmd.Parameters.Add(param);

                cmd.ExecuteNonQuery();
            }
            conn.Close();
        }
    }

    public string GetNickname()
    {
        string nickname = "";

        using (IDbConnection conn = new SqliteConnection(dbPath))
        {
            conn.Open();
            using (IDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT nickname FROM PlayerProfile WHERE id = 1;";
                using (IDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        nickname = reader.GetString(0);
                    }
                }
            }
            conn.Close();
        }

        return nickname;
    }
}