namespace TechCosmos.Hub.Editor
{
    internal static class GlueRecipeDocs
    {
        public static string GetDoc(GlueRecipeEntry recipe)
        {
            if (recipe != null && !string.IsNullOrWhiteSpace(recipe.doc))
                return recipe.doc;
            return GetDocById(recipe?.id);
        }

        private static string GetDocById(string recipeId)
        {
            return recipeId switch
            {
                "integration-events" =>
                    "定义项目层 EventBus 事件 struct：\n\n" +
                    "- CastRequestedEvent — 输入已接收，等待选目标\n" +
                    "- CastConfirmedEvent — 选目标完成，即将触发技能\n" +
                    "- CombatDamageAppliedEvent — 战斗伤害已结算\n\n" +
                    "这些类型位于项目命名空间，各框架包本身不认识它们。",

                "entity-registry" =>
                    "GameEntityRegistry 维护 EntityHandle 与 Unity 对象、战斗接口的映射。\n\n" +
                    "- Register / Unregister\n" +
                    "- TryGetTransform / TryGetOwner\n" +
                    "- 内嵌 CombatEntityRegistry 供 CombatSystem 使用",

                "composition-root" =>
                    "GameCompositionRoot 是项目组合根，唯一持有各框架 Hub 单例：\n\n" +
                    "- InputHub\n- TargetingHub\n- CombatHub\n- IEventBus\n- GameEntityRegistry\n\n" +
                    "各 Tech-Cosmos 卫星包内不应出现此类。",

                "cast-flow" =>
                    "CastFlowAdapter 实现完整施法管线：\n" +
                    "InputIntent → TargetRequest → TargetResult → SkillContext → TriggerEvent\n\n" +
                    "需配置 CastActionDefinition 将 actionId 映射到 triggerEvent 与 TargetMode。\n" +
                    "Hero 类型在 Hub 胶水页可配置（默认 IntegrationHero）。",

                "entity-bridge" =>
                    "GameEntityBridge 挂在实体 GameObject 上：\n\n" +
                    "- 分配 EntityHandle\n" +
                    "- 实现 IDamageable / IHealable\n" +
                    "- 桥接到 Hero 组件的 ReceiveDamage / Heal",

                "controller-input" =>
                    "ControllerInputSource 将 Unity 键盘输入转为 InputIntent，\n" +
                    "通过 InputHub.Emit 送入施法流。\n\n" +
                    "在 Bootstrap 中 BindKey(KeyCode.Space, \"Attack\") 等。",

                _ => "（暂无扩展说明，可在 Hub Studio 中为该 Recipe 填写 doc 字段）"
            };
        }
    }
}
