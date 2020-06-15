using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CodeChallengeBot
{
    public static class Program
    {
        public static void Main()
        {
            var centerOfGalaxy = new Vector(15, 15, 15);

            // draft
            var draftOptions = ReadInput<DraftOptions>();
            var autoDraftChoice = new DraftChoice();
            SendOutput(autoDraftChoice);

            // battle
            while (true)
            {
                var state = ReadInput<BattleState>();
                var output = new BattleOutput {Message = $"I have {state.My.Count} ships and move to center of galaxy"};

                foreach (var ship in state.My)
                {
                    output.UserCommands.Add(new UserCommand
                    {
                        Command = "MOVE",
                        Parameters = new MoveCommandParameters(ship.Id, centerOfGalaxy)
                    });

                    var gun = ship.Equipment.OfType<GunBlock>().FirstOrDefault();
                    if (gun != null)
                        output.UserCommands.Add(new UserCommand
                        {
                            Command = "ATTACK",
                            Parameters = new AttackCommandParameters(ship.Id, gun.Name, centerOfGalaxy)
                        });
                }

                SendOutput(output);
            }

            // ReSharper disable once FunctionNeverReturns
        }

        #region io

        private static T ReadInput<T>()
        {
            var line = Console.ReadLine();
            return JsonConvert.DeserializeObject<T>(line ?? throw new Exception("input is null"));
        }

        private static void SendOutput(object output)
        {
            var outputStr = JsonConvert.SerializeObject(output);
            Console.WriteLine(outputStr);
        }

        #endregion

        #region primitives

        [JsonConverter(typeof(VectorJsonConverter))]
        public class Vector
        {
            public Vector(int x, int y, int z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            public int X { get; }
            public int Y { get; }
            public int Z { get; }
        }

        private class VectorJsonConverter : JsonConverter<Vector>
        {
            public override void WriteJson(JsonWriter writer, Vector value, JsonSerializer serializer)
            {
                if (value == null)
                {
                    writer.WriteNull();
                    return;
                }

                var stringVector = $"{value.X}/{value.Y}/{value.Z}";
                writer.WriteValue(stringVector);
            }

            public override Vector ReadJson(JsonReader reader, Type objectType, Vector existingValue,
                bool hasExistingValue,
                JsonSerializer serializer)
            {
                if (reader.Value == null) return null;

                if (reader.ValueType != typeof(string)) throw new Exception(reader.Value.ToString());

                var value = (string) reader.Value;
                var components = value.Split(new[] {'/'}, 3);
                if (components.Length != 3) throw new Exception(reader.Value.ToString());

                var x = ParseComponent(components[0]);
                var y = ParseComponent(components[1]);
                var z = ParseComponent(components[2]);

                return new Vector(x, y, z);
            }

            private static int ParseComponent(string componentValue)
            {
                if (!int.TryParse(componentValue, out var value)) throw new Exception(componentValue);

                return value;
            }
        }

        #endregion

        #region battle commands

        public abstract class CommandParameters
        {
        }

        [JsonObject]
        private class AttackCommandParameters : CommandParameters
        {
            public AttackCommandParameters(int id, string gunName, Vector target)
            {
                Id = id;
                Name = gunName;
                Target = target;
            }

            public int Id { get; }
            public string Name { get; }
            public Vector Target { get; }
        }

        [JsonObject]
        public class MoveCommandParameters : CommandParameters
        {
            public MoveCommandParameters(int shipId, Vector target)
            {
                Id = shipId;
                Target = target;
            }

            public int Id { get; }
            public Vector Target { get; }
        }

        [JsonObject]
        public class AccelerateCommandParameters : CommandParameters
        {
            public AccelerateCommandParameters(int id, Vector vector)
            {
                Id = id;
                Vector = vector;
            }

            public int Id { get; }
            public Vector Vector { get; }
        }

        [JsonObject]
        public class UserCommand
        {
            public string Command;
            public CommandParameters Parameters;
        }

        [JsonObject]
        public class BattleOutput
        {
            public string Message;
            public List<UserCommand> UserCommands = new List<UserCommand>();
        }

        #endregion

        #region draft commands

        [JsonObject]
        public class DraftChoice
        {
        }


        [JsonObject]
        public class DraftOptions
        {
        }

        #endregion

        #region battle state

        [JsonObject]
        public class BattleState
        {
            public List<FireInfo> FireInfos;
            public List<Ship> My;
            public List<Ship> Opponent;
        }

        [JsonObject]
        public class FireInfo
        {
            public EffectType EffectType;
            public Vector Source;
            public Vector Target;
        }

        [JsonObject]
        public class Ship
        {
            public int Energy;
            public List<EquipmentBlock> Equipment;
            public int? Health;
            public int Id;
            public Vector Position;
            public Vector Velocity;
        }

        #endregion

        #region equipment

        [JsonConverter(typeof(EquipmentBlockConverter))]
        [JsonObject]
        public abstract class EquipmentBlock
        {
            public string Name;
            public abstract EquipmentType Type { get; }
        }

        public enum EquipmentType
        {
            Energy,
            Gun,
            Engine,
            Health
        }


        private class EquipmentBlockConverter : JsonConverter<EquipmentBlock>
        {
            public override void WriteJson(JsonWriter writer, EquipmentBlock value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }

            public override EquipmentBlock ReadJson(
                JsonReader reader,
                Type objectType,
                EquipmentBlock existingValue,
                bool hasExistingValue,
                JsonSerializer serializer)
            {
                var obj = JObject.Load(reader);
                var type = obj[nameof(EquipmentBlock.Type)].ToObject<EquipmentType>();

                EquipmentBlock equipmentBlock;
                switch (type)
                {
                    case EquipmentType.Energy:
                        equipmentBlock = new EnergyBlock();
                        break;
                    case EquipmentType.Gun:
                        equipmentBlock = new GunBlock();
                        break;
                    case EquipmentType.Engine:
                        equipmentBlock = new EngineBlock();
                        break;
                    case EquipmentType.Health:
                        equipmentBlock = new HealthBlock();
                        break;
                    default:
                        throw new NotSupportedException($"Unknown equipment {obj}");
                }

                serializer.Populate(obj.CreateReader(), equipmentBlock);
                return equipmentBlock;
            }
        }

        [JsonObject]
        public class EnergyBlock : EquipmentBlock
        {
            public int IncrementPerTurn;
            public int MaxEnergy;
            public int StartEnergy;
            public override EquipmentType Type => EquipmentType.Energy;
        }

        [JsonObject]
        public class EngineBlock : EquipmentBlock
        {
            public int MaxAccelerate;
            public override EquipmentType Type => EquipmentType.Engine;
        }

        [JsonObject]
        public class GunBlock : EquipmentBlock
        {
            public int Damage;
            public EffectType EffectType;
            public int EnergyPrice;
            public int Radius;
            public override EquipmentType Type => EquipmentType.Gun;
        }

        public enum EffectType
        {
            Blaster = 0
        }

        [JsonObject]
        public class HealthBlock : EquipmentBlock
        {
            public int MaxHealth;
            public int StartHealth;
            public override EquipmentType Type => EquipmentType.Health;
        }

        #endregion
    }
}