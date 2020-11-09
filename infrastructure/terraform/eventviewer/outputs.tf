output "event_viewer_subscription_endpoint" {
  value = "https://${var.event_viewer_appname}.azurewebsites.net/api/eventgrid"
}

output "event_viewer_base_endpoint" {
  value = "https://${var.event_viewer_appname}.azurewebsites.net"
}

output "event_viewer_appname" {
  value = var.event_viewer_appname
}

output "event_viewer_resource_group_name" {
  value = var.resource_group_name
}