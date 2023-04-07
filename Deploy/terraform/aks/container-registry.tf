resource "azurerm_container_registry" "acr" {
  count = var.acr.enabled ? 1 : 0

  name                = var.acr.name
  resource_group_name = local.rg
  location            = var.location
  sku                 = var.acr.sku
  admin_enabled       = var.acr.admin_enabled
}

resource "kubernetes_secret" "image_pull_secret" {
  count = var.acr.enabled && var.acr.admin_enabled && var.acr.create_pull_secret ? 1 : 0

  metadata {
    name = var.acr.pull_secret_name
  }

  type = "kubernetes.io/dockerconfigjson"

  data = {
    ".dockerconfigjson" = jsonencode({
      auths = {
        "${azurerm_container_registry.acr.0.login_server}" = {
          "username" = azurerm_container_registry.acr.0.admin_username
          "password" = azurerm_container_registry.acr.0.admin_password
          "auth"     = base64encode("${azurerm_container_registry.acr.0.admin_username}:${azurerm_container_registry.acr.0.admin_password}")
        }
      }
    })
  }

  depends_on = [
    azurerm_container_registry.acr
  ]
}

resource "azurerm_role_assignment" "acr_role_assignment" {
  count = var.acr.enabled ? 1 : 0

  scope                = azurerm_container_registry.acr.0.id
  principal_id         = data.azurerm_user_assigned_identity.agent_pool_identity.principal_id
  role_definition_name = "AcrPull"

  depends_on = [
    azurerm_container_registry.acr
  ]
}
