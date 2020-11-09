locals {
  test_code       = "test-code-verify"
  verified_output = data.external.eventgrid_subscription_verify.result.code == local.test_code ? toset(var.scope_ids) : []
}

data "external" "eventgrid_subscription_verify" {
  program = ["bash", "${path.module}/test-endpoint.sh"]
  query = {
    url      = var.endpoint
    testcode = local.test_code
  }
}

# Subscription
resource "azurerm_eventgrid_event_subscription" "eventgrid_subscription" {
  for_each = var.subscriptions_disabled ? toset([]) : var.fail_gracefully ? local.verified_output : toset(var.scope_ids)
  name     = "${var.name}${index(var.scope_ids, each.value)}"
  scope    = each.value
  webhook_endpoint {
    url = var.endpoint
  }

  dynamic "storage_blob_dead_letter_destination" {
    # We just need a way of deciding if we want to add the dead letter, so "["1"]" works
    for_each = var.dead_letter_sa_id != null && var.dead_letter_container_name != null ? ["1"] : []
    content {
      storage_account_id          = var.dead_letter_sa_id
      storage_blob_container_name = var.dead_letter_container_name
    }
  }
}